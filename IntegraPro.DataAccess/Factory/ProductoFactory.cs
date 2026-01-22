using IntegraPro.DataAccess.Dao;
using IntegraPro.DataAccess.Mappers;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class ProductoFactory : MasterDao
{
    private readonly ProductoMapper _mapper;

    public ProductoFactory(string connectionString) : base(connectionString)
    {
        _mapper = new ProductoMapper();
    }

    public List<ProductoDTO> GetAll()
    {
        var dt = ExecuteQuery("SELECT * FROM PRODUCTO WHERE activo = 1", null, false);
        var lista = new List<ProductoDTO>();
        foreach (DataRow row in dt.Rows) lista.Add(_mapper.MapFromRow(row));
        return lista;
    }

    public ProductoDTO? GetById(int id)
    {
        var parameters = new[] { new SqlParameter("@id", id) };
        var dt = ExecuteQuery("SELECT * FROM PRODUCTO WHERE id = @id", parameters, false);
        return dt.Rows.Count > 0 ? _mapper.MapFromRow(dt.Rows[0]) : null;
    }

    public int Create(ProductoDTO producto)
    {
        object result = ExecuteScalar("sp_Producto_Insert", _mapper.MapToParameters(producto).ToArray(), true);
        return Convert.ToInt32(result);
    }

    public void Update(ProductoDTO producto)
    {
        ExecuteNonQuery("sp_Producto_Update", _mapper.MapToParameters(producto), true);
    }

    // Registra un ingrediente para un producto elaborado
    public void InsertarComposicion(int padreId, int materialId, decimal cantidad)
    {
        var parameters = new[]
        {
            new SqlParameter("@producto_padre_id", padreId),
            new SqlParameter("@material_id", materialId),
            new SqlParameter("@cantidad_necesaria", cantidad)
        };

        // 
        string sql = "INSERT INTO PRODUCTO_COMPOSICION (producto_padre_id, material_id, cantidad_necesaria) VALUES (@producto_padre_id, @material_id, @cantidad_necesaria)";

        ExecuteNonQuery(sql, parameters, false);
    }

    // Obtiene la lista de materiales para que el InventarioService descuente stock
    // Dentro de ProductoFactory.cs
    public List<ProductoComposicionDTO> ObtenerComposicion(int padreId)
    {
        var parameters = new[] { new SqlParameter("@id", padreId) };

        // Cambia 'material_id' por el nombre real de tu columna si es diferente
        var dt = ExecuteQuery("SELECT material_id, cantidad_necesaria FROM PRODUCTO_COMPOSICION WHERE producto_padre_id = @id", parameters, false);

        var lista = new List<ProductoComposicionDTO>();
        foreach (DataRow row in dt.Rows)
        {
            lista.Add(new ProductoComposicionDTO
            {
                // Mapeo exacto a tu clase ProductoComposicionDTO
                MaterialId = Convert.ToInt32(row["material_id"]),
                CantidadNecesaria = Convert.ToDecimal(row["cantidad_necesaria"])
            });
        }
        return lista;
    }

    public List<ProductoDTO> GetStockAlerts()
    {
        var dt = ExecuteQuery("SELECT * FROM VW_ALERTA_STOCK_BAJO", null, false);
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