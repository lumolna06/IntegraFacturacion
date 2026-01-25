using IntegraPro.DataAccess.Dao;
using IntegraPro.DataAccess.Mappers;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class ProductoFactory(string connectionString) : MasterDao(connectionString)
{
    private readonly ProductoMapper _mapper = new();

    /// <summary>
    /// Obtiene todos los productos filtrando por sucursal si el usuario tiene restricciones.
    /// </summary>
    public List<ProductoDTO> GetAll(UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("productos"); // Validación preventiva de módulo

        string sql = "SELECT p.* FROM PRODUCTO p";
        SqlParameter[]? parameters = null;

        if (ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " INNER JOIN PRODUCTO_SUCURSAL ps ON p.id = ps.producto_id WHERE p.activo = 1 AND ps.sucursal_id = @sucId";
            parameters = [new SqlParameter("@sucId", ejecutor.SucursalId)];
        }
        else
        {
            sql += " WHERE p.activo = 1";
        }

        var dt = ExecuteQuery(sql, parameters, false);
        var lista = new List<ProductoDTO>();
        foreach (DataRow row in dt.Rows) lista.Add(_mapper.MapFromRow(row));
        return lista;
    }

    public ProductoDTO? GetById(int id, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("productos");

        string sql = "SELECT p.* FROM PRODUCTO p";
        List<SqlParameter> parameters = [new SqlParameter("@id", id)];

        if (ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " INNER JOIN PRODUCTO_SUCURSAL ps ON p.id = ps.producto_id WHERE p.id = @id AND ps.sucursal_id = @sucId";
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

        if (ejecutor.TienePermiso("sucursal_limit"))
            producto.SucursalId = ejecutor.SucursalId;

        object result = ExecuteScalar("sp_Producto_Insert", _mapper.MapToParameters(producto).ToArray(), true);
        return Convert.ToInt32(result);
    }

    public void Update(ProductoDTO producto, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("productos");
        ejecutor.ValidarEscritura();

        if (ejecutor.TienePermiso("sucursal_limit"))
        {
            // Validamos que el producto realmente pertenezca a la sucursal del usuario antes de actualizar
            var prodActual = GetById(producto.Id, ejecutor);
            if (prodActual == null)
                throw new UnauthorizedAccessException("No tiene permisos para modificar este producto en su sucursal.");

            producto.SucursalId = ejecutor.SucursalId;
        }

        ExecuteNonQuery("sp_Producto_Update", _mapper.MapToParameters(producto).ToArray(), true);
    }

    public void InsertarComposicion(int padreId, int materialId, decimal cantidad, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("productos");
        ejecutor.ValidarEscritura();

        var parameters = new[]
        {
            new SqlParameter("@producto_padre_id", padreId),
            new SqlParameter("@material_id", materialId),
            new SqlParameter("@cantidad_necesaria", cantidad)
        };

        string sql = "INSERT INTO PRODUCTO_COMPOSICION (producto_padre_id, material_id, cantidad_necesaria) VALUES (@producto_padre_id, @material_id, @cantidad_necesaria)";
        ExecuteNonQuery(sql, parameters, false);
    }

    public List<ProductoComposicionDTO> ObtenerComposicion(int padreId)
    {
        // Este método suele ser interno para procesos de producción, no requiere validación de escritura
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

    public List<ProductoDTO> GetStockAlerts(UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("productos");

        string sql = @"SELECT p.id, p.nombre, p.stock_minimo, ps.existencia 
                       FROM PRODUCTO p 
                       INNER JOIN PRODUCTO_SUCURSAL ps ON p.id = ps.producto_id 
                       WHERE ps.existencia <= p.stock_minimo 
                       AND p.activo = 1 AND p.es_servicio = 0";

        List<SqlParameter> parameters = new();

        if (ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " AND ps.sucursal_id = @sucId";
            parameters.Add(new SqlParameter("@sucId", ejecutor.SucursalId));
        }

        var dt = ExecuteQuery(sql, parameters.Count > 0 ? parameters.ToArray() : null, false);
        var lista = new List<ProductoDTO>();
        foreach (DataRow row in dt.Rows)
        {
            lista.Add(new ProductoDTO
            {
                Id = Convert.ToInt32(row["id"]),
                Nombre = row["nombre"].ToString() ?? "",
                Existencia = Convert.ToDecimal(row["existencia"]),
                StockMinimo = Convert.ToDecimal(row["stock_minimo"])
            });
        }
        return lista;
    }
}