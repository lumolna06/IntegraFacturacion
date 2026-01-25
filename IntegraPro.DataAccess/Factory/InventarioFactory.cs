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
    /// Inserta un movimiento de inventario validando permisos de escritura y sucursal.
    /// </summary>
    public bool Insertar(MovimientoInventarioDTO dto, UsuarioDTO ejecutor)
    {
        // 1. VALIDACIÓN DE PERMISOS
        // Roles permitidos: Administrador (all), Adm. Sucursal (inventario), Adm. Inventario (inventario)
        if (!ejecutor.TienePermiso("inventario"))
            throw new UnauthorizedAccessException("No tiene permisos para realizar movimientos de inventario.");

        if (ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Perfil de solo lectura: No puede alterar el stock.");

        // 2. SEGURIDAD DE SUCURSAL (sucursal_limit)
        // Si el usuario está limitado, forzamos que el movimiento sea en SU sucursal
        if (ejecutor.TienePermiso("sucursal_limit"))
            dto.SucursalId = ejecutor.SucursalId;

        string sql = @"INSERT INTO MOVIMIENTO_INVENTARIO 
                       (producto_id, usuario_id, sucursal_id, tipo_movimiento, cantidad, documento_referencia, notas) 
                       VALUES (@pid, @uid, @sid, @tipo, @cant, @ref, @not)";

        var p = new[] {
            new SqlParameter("@pid", dto.ProductoId),
            new SqlParameter("@uid", ejecutor.Id), // Usamos el ID del ejecutor real
            new SqlParameter("@sid", dto.SucursalId),
            new SqlParameter("@tipo", dto.TipoMovimiento.ToUpper()),
            new SqlParameter("@cant", dto.Cantidad),
            new SqlParameter("@ref", (object?)dto.DocumentoReferencia ?? DBNull.Value),
            new SqlParameter("@not", (object?)dto.Notas ?? DBNull.Value)
        };

        try
        {
            // Nota: Se asume que existe un TRIGGER en DB que actualiza PRODUCTO_SUCURSAL
            ExecuteNonQuery(sql, p, false);
            return true;
        }
        catch (Exception ex)
        {
            // Aquí podrías loguear el error (ex.Message)
            return false;
        }
    }

    /// <summary>
    /// Consulta el historial. Si el ejecutor tiene sucursal_limit, solo verá su sede.
    /// </summary>
    public DataTable ObtenerHistorial(UsuarioDTO ejecutor, int? productoId, DateTime? desde, DateTime? hasta)
    {
        // 1. VALIDACIÓN DE PERMISOS DE LECTURA
        if (!ejecutor.TienePermiso("inventario") && !ejecutor.TienePermiso("auditoria"))
            return new DataTable(); // Retorna tabla vacía si no tiene permiso

        // 2. DETERMINAR SUCURSAL A CONSULTAR
        int? sIdFiltro = ejecutor.TienePermiso("sucursal_limit") ? ejecutor.SucursalId : null;

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

        // Aplicamos el blindaje de sucursal
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
            // Ajuste para incluir todo el día final
            parametros.Add(new SqlParameter("@hasta", hasta.Value.Date.AddDays(1).AddSeconds(-1)));
        }

        sql += " ORDER BY m.fecha DESC";

        return ExecuteQuery(sql, parametros.ToArray(), false);
    }
}