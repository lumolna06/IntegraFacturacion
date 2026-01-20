using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class CategoriaFactory : MasterDao
{
    public CategoriaFactory(string connectionString) : base(connectionString) { }

    public List<CategoriaDTO> GetAll()
    {
        // Usamos false porque es una consulta de texto plano, no un SP
        var dt = ExecuteQuery("SELECT * FROM CATEGORIA", null, false);
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

    public bool Create(CategoriaDTO dto)
    {
        var parameters = new[] {
            new SqlParameter("@nombre", dto.Nombre),
            new SqlParameter("@descripcion", dto.Descripcion ?? (object)DBNull.Value)
        };
        // Ejecutamos como consulta de texto (false)
        ExecuteNonQuery("INSERT INTO CATEGORIA (nombre, descripcion) VALUES (@nombre, @descripcion)", parameters, false);
        return true;
    }
}