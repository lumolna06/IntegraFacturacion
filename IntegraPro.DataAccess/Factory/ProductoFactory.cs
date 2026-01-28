using IntegraPro.DataAccess.Dao;
using IntegraPro.DataAccess.Mappers;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System;

namespace IntegraPro.DataAccess.Factory;

public class ProductoFactory(string connectionString) : MasterDao(connectionString)
{
    private readonly ProductoMapper _mapper = new();

    public List<ProductoDTO> GetAll(UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("productos");

        string sql;
        List<SqlParameter> parameters = new();

        if (!ejecutor.TienePermiso("all") && ejecutor.TienePermiso("sucursal_limit"))
        {
            sql = @"SELECT p.*, ps.sucursal_id, s.nombre as sucursal_nombre, ps.existencia as existencia_local 
                FROM PRODUCTO p 
                INNER JOIN PRODUCTO_SUCURSAL ps ON p.id = ps.producto_id 
                INNER JOIN SUCURSAL s ON ps.sucursal_id = s.id
                WHERE p.activo = 1 AND ps.sucursal_id = @sucId";

            parameters.Add(new SqlParameter("@sucId", ejecutor.SucursalId));
        }
        else
        {
            sql = @"SELECT p.*, 
                           ps.sucursal_id, 
                           s.nombre as sucursal_nombre,
                           ps.existencia as existencia_local,
                           (SELECT SUM(existencia) FROM PRODUCTO_SUCURSAL WHERE producto_id = p.id) as existencia_total
                FROM PRODUCTO p
                LEFT JOIN PRODUCTO_SUCURSAL ps ON p.id = ps.producto_id
                LEFT JOIN SUCURSAL s ON ps.sucursal_id = s.id
                WHERE p.activo = 1";
        }

        var dt = ExecuteQuery(sql, parameters.Count > 0 ? parameters.ToArray() : null, false);
        var lista = new List<ProductoDTO>();

        foreach (DataRow row in dt.Rows)
        {
            var dto = _mapper.MapFromRow(row);

            if (row.Table.Columns.Contains("sucursal_nombre") && row["sucursal_nombre"] != DBNull.Value)
            {
                dto.Descripcion = $"[Sede: {row["sucursal_nombre"]}] " + (dto.Descripcion ?? "");
            }

            if (row.Table.Columns.Contains("existencia_total"))
            {
                dto.Existencia = row["existencia_total"] != DBNull.Value
                                 ? Convert.ToDecimal(row["existencia_total"])
                                 : 0m;
            }

            lista.Add(dto);
        }
        return lista;
    }

    public ProductoDTO? GetById(int id, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("productos");

        string sql = @"SELECT p.*, ps.sucursal_id, ps.existencia as existencia_local 
                       FROM PRODUCTO p 
                       LEFT JOIN PRODUCTO_SUCURSAL ps ON p.id = ps.producto_id";

        List<SqlParameter> parameters = [new SqlParameter("@id", id)];

        if (!ejecutor.TienePermiso("all") && ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " WHERE p.id = @id AND ps.sucursal_id = @sucId";
            parameters.Add(new SqlParameter("@sucId", ejecutor.SucursalId));
        }
        else
        {
            sql += " WHERE p.id = @id";
        }

        var dt = ExecuteQuery(sql, parameters.ToArray(), false);
        return dt.Rows.Count > 0 ? _mapper.MapFromRow(dt.Rows[0]) : null;
    }

    public int Create(ProductoDTO producto, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("productos");
        ejecutor.ValidarEscritura();

        if (!ejecutor.TienePermiso("all") && ejecutor.TienePermiso("sucursal_limit"))
            producto.SucursalId = ejecutor.SucursalId;

        object result = ExecuteScalar("sp_Producto_Insert", _mapper.MapToParameters(producto).ToArray(), true);
        return Convert.ToInt32(result);
    }

    public void Update(ProductoDTO producto, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("productos");
        ejecutor.ValidarEscritura();

        if (!ejecutor.TienePermiso("all") && ejecutor.TienePermiso("sucursal_limit"))
        {
            var prodActual = GetById(producto.Id, ejecutor);
            if (prodActual == null)
                throw new UnauthorizedAccessException("No tiene permisos para modificar este producto en su sucursal.");

            producto.SucursalId = ejecutor.SucursalId;
        }

        ExecuteNonQuery("sp_Producto_Update", _mapper.MapToParameters(producto).ToArray(), true);
    }

    public List<ProductoDTO> GetStockAlerts(UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("productos");

        // Cambiamos p.id, p.nombre por p.* para que el Mapper no falle por columnas faltantes
        string sql = @"SELECT p.*, ps.existencia as existencia_local, ps.sucursal_id 
                       FROM PRODUCTO p 
                       INNER JOIN PRODUCTO_SUCURSAL ps ON p.id = ps.producto_id 
                       WHERE ps.existencia <= p.stock_minimo 
                       AND p.activo = 1 AND p.es_servicio = 0";

        List<SqlParameter> parameters = new();

        if (!ejecutor.TienePermiso("all") && ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " AND ps.sucursal_id = @sucId";
            parameters.Add(new SqlParameter("@sucId", ejecutor.SucursalId));
        }

        var dt = ExecuteQuery(sql, parameters.Count > 0 ? parameters.ToArray() : null, false);
        var lista = new List<ProductoDTO>();
        foreach (DataRow row in dt.Rows)
        {
            // Ahora el mapper recibirá todas las columnas y no dará error de DBNull
            lista.Add(_mapper.MapFromRow(row));
        }
        return lista;
    }

    public void InsertarComposicion(int padreId, int materialId, decimal cantidad, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("productos");
        ejecutor.ValidarEscritura();
        var parameters = new[] {
            new SqlParameter("@producto_padre_id", padreId),
            new SqlParameter("@material_id", materialId),
            new SqlParameter("@cantidad_necesaria", cantidad)
        };
        ExecuteNonQuery("INSERT INTO PRODUCTO_COMPOSICION (producto_padre_id, material_id, cantidad_necesaria) VALUES (@producto_padre_id, @material_id, @cantidad_necesaria)", parameters, false);
    }

    public List<ProductoComposicionDTO> ObtenerComposicion(int padreId)
    {
        var parameters = new[] { new SqlParameter("@id", padreId) };
        var dt = ExecuteQuery("SELECT material_id, cantidad_necesaria FROM PRODUCTO_COMPOSICION WHERE producto_padre_id = @id", parameters, false);
        var lista = new List<ProductoComposicionDTO>();
        foreach (DataRow row in dt.Rows)
        {
            lista.Add(new ProductoComposicionDTO
            {
                MaterialId = Convert.ToInt32(row["material_id"]),
                CantidadNecesaria = Convert.ToDecimal(row["cantidad_necesaria"])
            });
        }
        return lista;
    }
}