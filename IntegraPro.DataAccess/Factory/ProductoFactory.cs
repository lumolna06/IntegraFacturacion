
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
        var dt = ExecuteQuery("SELECT * FROM PRODUCTO WHERE activo = 1");
        var lista = new List<ProductoDTO>();
        foreach (DataRow row in dt.Rows) lista.Add(_mapper.MapFromRow(row));
        return lista;
    }

    public ProductoDTO? GetById(int id)
    {
        var parameters = new[] { new SqlParameter("@id", id) };
        var dt = ExecuteQuery("SELECT * FROM PRODUCTO WHERE id = @id", parameters);
        return dt.Rows.Count > 0 ? _mapper.MapFromRow(dt.Rows[0]) : null;
    }

    public int Create(ProductoDTO producto)
    {
        // Nota: Asegúrate de tener el SP sp_Producto_Insert en SQL que haga el INSERT y devuelva SCOPE_IDENTITY()
        return ExecuteScalar("sp_Producto_Insert", _mapper.MapToParameters(producto));
    }

    public void Update(ProductoDTO producto)
    {
        ExecuteNonQuery("sp_Producto_Update", _mapper.MapToParameters(producto), true);
    }

    public void InsertarComposicion(int padreId, int materialId, decimal cantidad)
    {
        var parameters = new[]
        {
        new SqlParameter("@producto_padre_id", padreId),
        new SqlParameter("@material_id", materialId),
        new SqlParameter("@cantidad_necesaria", cantidad)
    };

        // Agregamos el "false" al final para indicar que NO es un Stored Procedure
        ExecuteNonQuery("INSERT INTO PRODUCTO_COMPOSICION (producto_padre_id, material_id, cantidad_necesaria) VALUES (@producto_padre_id, @material_id, @cantidad_necesaria)", parameters, false);
    }

    public List<ProductoDTO> GetStockAlerts()
    {
        var dt = ExecuteQuery("SELECT * FROM VW_ALERTA_STOCK_BAJO");
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