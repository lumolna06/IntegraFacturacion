using IntegraPro.DataAccess.Dao; 
using IntegraPro.DTO.Models;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class CategoriaFactory : MasterDao
{
    public CategoriaFactory(string connectionString) : base(connectionString) { }

    public List<CategoriaDTO> GetAll()
    {
        var dt = ExecuteQuery("SELECT * FROM CATEGORIA");
        var lista = new List<CategoriaDTO>();
        foreach (DataRow row in dt.Rows)
        {
            lista.Add(new CategoriaDTO
            {
                Id = Convert.ToInt32(row["id"]),
                Nombre = row["nombre"].ToString() ?? "",
                Descripcion = row["descripcion"]?.ToString()
            });
        }
        return lista;
    }
}