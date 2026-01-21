using Microsoft.Data.SqlClient;
using IntegraPro.DTO.Models;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class ProveedorFactory(string connectionString)
{
    private readonly string _connectionString = connectionString;

    public List<ProveedorDTO> ObtenerTodos()
    {
        var lista = new List<ProveedorDTO>();
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SELECT * FROM PROVEEDOR", conn);
        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new ProveedorDTO
            {
                Id = (int)reader["id"],
                Identificacion = reader["identificacion"].ToString()!,
                Nombre = reader["nombre"].ToString()!,
                Correo = reader["correo"]?.ToString(),
                Telefono = reader["telefono"]?.ToString(),
                Activo = (bool)reader["activo"]
            });
        }
        return lista;
    }

    public void Crear(ProveedorDTO proveedor)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(
            "INSERT INTO PROVEEDOR (identificacion, nombre, correo, telefono, activo) VALUES (@ide, @nom, @cor, @tel, @act)", conn);

        cmd.Parameters.AddWithValue("@ide", proveedor.Identificacion);
        cmd.Parameters.AddWithValue("@nom", proveedor.Nombre);
        cmd.Parameters.AddWithValue("@cor", (object?)proveedor.Correo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@tel", (object?)proveedor.Telefono ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@act", proveedor.Activo);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void Actualizar(ProveedorDTO proveedor)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(
            @"UPDATE PROVEEDOR 
              SET identificacion = @ide, nombre = @nom, correo = @cor, telefono = @tel, activo = @act 
              WHERE id = @id", conn);

        cmd.Parameters.AddWithValue("@id", proveedor.Id);
        cmd.Parameters.AddWithValue("@ide", proveedor.Identificacion);
        cmd.Parameters.AddWithValue("@nom", proveedor.Nombre);
        cmd.Parameters.AddWithValue("@cor", (object?)proveedor.Correo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@tel", (object?)proveedor.Telefono ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@act", proveedor.Activo);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void Eliminar(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        // OPCIÓN 1: Borrado Lógico (Recomendado para ERP)
        using var cmd = new SqlCommand("UPDATE PROVEEDOR SET activo = 0 WHERE id = @id", conn);

        // OPCIÓN 2: Borrado Físico (Descomenta la línea de abajo si prefieres borrar de verdad)
        // using var cmd = new SqlCommand("DELETE FROM PROVEEDOR WHERE id = @id", conn);

        cmd.Parameters.AddWithValue("@id", id);
        conn.Open();
        cmd.ExecuteNonQuery();
    }
}