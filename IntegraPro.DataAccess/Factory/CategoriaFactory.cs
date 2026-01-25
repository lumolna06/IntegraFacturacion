using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class CategoriaFactory(string connectionString) : MasterDao(connectionString)
{
    public List<CategoriaDTO> GetAll(UsuarioDTO ejecutor)
    {
        // SEGURIDAD: Validar que el usuario tiene permiso para ver este módulo
        ejecutor.ValidarAcceso("inventario"); // O "categorias", según tu JSON

        // Las categorías suelen ser globales, por lo que no aplicamos GetFiltroSucursal 
        // a menos que tu tabla CATEGORIA tenga una columna sucursal_id.
        var dt = ExecuteQuery("SELECT * FROM CATEGORIA", null, false);
        var lista = new List<CategoriaDTO>();

        if (dt == null) return lista;

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

    public bool Create(CategoriaDTO dto, UsuarioDTO ejecutor)
    {
        // SEGURIDAD: Reemplazamos el 'if' manual por los helpers del DTO
        ejecutor.ValidarAcceso("inventario");
        ejecutor.ValidarEscritura();

        var parameters = new[] {
            new SqlParameter("@nombre", dto.Nombre),
            new SqlParameter("@descripcion", (object?)dto.Descripcion ?? DBNull.Value)
        };

        ExecuteNonQuery("INSERT INTO CATEGORIA (nombre, descripcion) VALUES (@nombre, @descripcion)", parameters, false);
        return true;
    }
}