using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class FacturaFactory(string connectionString) : MasterDao(connectionString)
{
    // ... Métodos CrearEncabezado e InsertarDetalle se mantienen igual ...

    /// <summary>
    /// Lista facturas aplicando filtros de búsqueda, cliente, condición y sucursal.
    /// </summary>
    public DataTable Listar(DateTime inicio, DateTime fin, int? clienteId, string buscar, string condicion, UsuarioDTO ejecutor)
    {
        // 1. Base del SQL con JOIN para traer nombre de cliente
        string sql = @"SELECT F.*, C.nombre as ClienteNombre 
                       FROM FACTURA_ENCABEZADO F
                       LEFT JOIN CLIENTE C ON F.cliente_id = C.id
                       WHERE F.fecha BETWEEN @inicio AND @fin";

        var pars = new List<SqlParameter> {
            new SqlParameter("@inicio", inicio),
            new SqlParameter("@fin", fin)
        };

        // 2. Filtro de Seguridad: sucursal_limit
        if (ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " AND F.sucursal_id = @sid";
            pars.Add(new SqlParameter("@sid", ejecutor.SucursalId));
        }

        // 3. Filtro Opcional: Cliente específico
        if (clienteId.HasValue && clienteId > 0)
        {
            sql += " AND F.cliente_id = @cid";
            pars.Add(new SqlParameter("@cid", clienteId.Value));
        }

        // 4. Filtro Opcional: Condición (Contado/Crédito)
        if (!string.IsNullOrEmpty(condicion))
        {
            sql += " AND F.condicion_venta = @cond";
            pars.Add(new SqlParameter("@cond", condicion));
        }

        // 5. Filtro Opcional: Búsqueda abierta (Consecutivo, Clave o Nombre Cliente)
        if (!string.IsNullOrEmpty(buscar))
        {
            sql += " AND (F.consecutivo LIKE @bus OR F.clave_numerica LIKE @bus OR C.nombre LIKE @bus)";
            pars.Add(new SqlParameter("@bus", "%" + buscar + "%"));
        }

        sql += " ORDER BY F.id DESC";

        return ExecuteQuery(sql, pars.ToArray(), false);
    }

    // ... Resto de métodos (ObtenerPorId, ActualizarEstadoHacienda) se mantienen igual ...
}