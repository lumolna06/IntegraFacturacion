using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class InventarioFactory(string connectionString) : MasterDao(connectionString)
{
    public bool Insertar(MovimientoInventarioDTO dto, UsuarioDTO ejecutor)
    {
        // 1. SEGURIDAD BÁSICA
        ejecutor.ValidarAcceso("inventario");
        ejecutor.ValidarEscritura();

        // 2. LÓGICA DE PRIORIDAD DE SUCURSAL (EL CORAZÓN DEL CAMBIO)
        int sucursalDestino;

        // Si es 'all', el administrador manda y el JSON manda.
        if (ejecutor.TienePermiso("all"))
        {
            sucursalDestino = dto.SucursalId; // Aquí ya tomará el 3 del JSON
        }
        // Si NO es 'all' pero tiene límite, forzamos su sede.
        else if (ejecutor.TienePermiso("sucursal_limit"))
        {
            sucursalDestino = ejecutor.SucursalId;
        }
        // Caso por defecto (seguridad)
        else
        {
            sucursalDestino = dto.SucursalId;
        }

        string sql = @"INSERT INTO MOVIMIENTO_INVENTARIO 
                       (producto_id, usuario_id, sucursal_id, tipo_movimiento, cantidad, documento_referencia, notas) 
                       VALUES (@pid, @uid, @sid, @tipo, @cant, @ref, @not)";

        var p = new[] {
            new SqlParameter("@pid", dto.ProductoId),
            new SqlParameter("@uid", ejecutor.Id),
            new SqlParameter("@sid", sucursalDestino), // Usamos la variable calculada
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
            throw;
        }
    }

    public DataTable ObtenerHistorial(UsuarioDTO ejecutor, int? productoId, DateTime? desde, DateTime? hasta)
    {
        if (!ejecutor.TienePermiso("inventario") && !ejecutor.TienePermiso("auditoria"))
            throw new UnauthorizedAccessException("No tiene permisos para consultar el historial.");

        // Aplicamos la misma jerarquía para la visualización
        int? sIdFiltro = null;
        if (!ejecutor.TienePermiso("all") && ejecutor.TienePermiso("sucursal_limit"))
        {
            sIdFiltro = ejecutor.SucursalId;
        }

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
            parametros.Add(new SqlParameter("@hasta", hasta.Value.Date.AddDays(1).AddSeconds(-1)));
        }

        sql += " ORDER BY m.fecha DESC";

        return ExecuteQuery(sql, parametros.ToArray(), false);
    }
}