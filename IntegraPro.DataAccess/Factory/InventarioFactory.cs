using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class InventarioFactory(string connectionString) : MasterDao(connectionString)
{
    /// <summary>
    /// Inserta un movimiento de inventario validando permisos y forzando la sucursal del ejecutor.
    /// </summary>
    public bool Insertar(MovimientoInventarioDTO dto, UsuarioDTO ejecutor)
    {
        // 1. SEGURIDAD ESTÁNDAR
        ejecutor.ValidarAcceso("inventario");
        ejecutor.ValidarEscritura();

        // 2. SEGURIDAD DE SUCURSAL
        // Si el usuario tiene restricción de sucursal, ignoramos lo que venga en el DTO 
        // y forzamos su propia SucursalId para evitar que afecte stock de otras sedes.
        int sucursalDestino = ejecutor.TienePermiso("sucursal_limit")
            ? ejecutor.SucursalId
            : dto.SucursalId;

        string sql = @"INSERT INTO MOVIMIENTO_INVENTARIO 
                       (producto_id, usuario_id, sucursal_id, tipo_movimiento, cantidad, documento_referencia, notas) 
                       VALUES (@pid, @uid, @sid, @tipo, @cant, @ref, @not)";

        var p = new[] {
            new SqlParameter("@pid", dto.ProductoId),
            new SqlParameter("@uid", ejecutor.Id),
            new SqlParameter("@sid", sucursalDestino),
            new SqlParameter("@tipo", dto.TipoMovimiento.ToUpper()),
            new SqlParameter("@cant", dto.Cantidad),
            new SqlParameter("@ref", (object?)dto.DocumentoReferencia ?? DBNull.Value),
            new SqlParameter("@not", (object?)dto.Notas ?? DBNull.Value)
        };

        try
        {
            ExecuteNonQuery(sql, p, false);
            return true;
        }
        catch (Exception)
        {
            // El error subirá al Service para ser encapsulado en el ApiResponse
            throw;
        }
    }

    /// <summary>
    /// Consulta el historial con filtrado automático por sucursal según el perfil.
    /// </summary>
    public DataTable ObtenerHistorial(UsuarioDTO ejecutor, int? productoId, DateTime? desde, DateTime? hasta)
    {
        // 1. SEGURIDAD: Debe tener permiso de inventario o auditoría para ver estos datos
        if (!ejecutor.TienePermiso("inventario") && !ejecutor.TienePermiso("auditoria"))
            throw new UnauthorizedAccessException("No tiene permisos para consultar el historial de movimientos.");

        // 2. FILTRADO AUTOMÁTICO DE SUCURSAL
        int? sIdFiltro = ejecutor.TienePermiso("sucursal_limit") ? (int?)ejecutor.SucursalId : null;

        string sql = @"SELECT m.*, p.nombre as ProductoNombre, u.username as UsuarioNombre, s.nombre as SucursalNombre
                       FROM MOVIMIENTO_INVENTARIO m
                       INNER JOIN PRODUCTO p ON m.producto_id = p.id
                       INNER JOIN USUARIO u ON m.usuario_id = u.id
                       INNER JOIN SUCURSAL s ON m.sucursal_id = s.id
                       WHERE 1=1";

        var parametros = new List<SqlParameter>();

        if (productoId.HasValue)
        {
            sql += " AND m.producto_id = @pid";
            parametros.Add(new SqlParameter("@pid", productoId.Value));
        }

        // Aplicamos el blindaje si el usuario es limitado
        if (sIdFiltro.HasValue)
        {
            sql += " AND m.sucursal_id = @sid";
            parametros.Add(new SqlParameter("@sid", sIdFiltro.Value));
        }

        if (desde.HasValue)
        {
            sql += " AND m.fecha >= @desde";
            parametros.Add(new SqlParameter("@desde", desde.Value));
        }

        if (hasta.HasValue)
        {
            sql += " AND m.fecha <= @hasta";
            // Normalización del fin del día (23:59:59)
            parametros.Add(new SqlParameter("@hasta", hasta.Value.Date.AddDays(1).AddSeconds(-1)));
        }

        sql += " ORDER BY m.fecha DESC";

        return ExecuteQuery(sql, parametros.ToArray(), false);
    }
}