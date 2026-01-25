using Microsoft.Data.SqlClient;
using IntegraPro.DTO.Models;
using System.Data;
using IntegraPro.DataAccess.Dao;

namespace IntegraPro.DataAccess.Factory;

// 1. Heredamos de MasterDao para usar la lógica centralizada
public class ProveedorFactory(string connectionString) : MasterDao(connectionString)
{
    public List<ProveedorDTO> ObtenerTodos(UsuarioDTO ejecutor)
    {
        // Validación de permiso (Rol 4 o Rol 1 suelen tener acceso a proveedores)
        if (!ejecutor.TienePermiso("proveedores") && !ejecutor.TienePermiso("compras"))
            return new List<ProveedorDTO>();

        // Usamos ExecuteQuery del MasterDao
        var dt = ExecuteQuery("SELECT * FROM PROVEEDOR WHERE activo = 1", null, false);
        var lista = new List<ProveedorDTO>();

        foreach (DataRow reader in dt.Rows)
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

    public void Crear(ProveedorDTO proveedor, UsuarioDTO ejecutor)
    {
        // 2. VALIDACIÓN DE SEGURIDAD
        if (!ejecutor.TienePermiso("compras") && !ejecutor.TienePermiso("proveedores"))
            throw new UnauthorizedAccessException("No tiene permisos para crear proveedores.");

        if (ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Cuenta de solo lectura.");

        string sql = "INSERT INTO PROVEEDOR (identificacion, nombre, correo, telefono, activo) VALUES (@ide, @nom, @cor, @tel, @act)";

        SqlParameter[] parameters = {
            new SqlParameter("@ide", proveedor.Identificacion),
            new SqlParameter("@nom", proveedor.Nombre),
            new SqlParameter("@cor", (object?)proveedor.Correo ?? DBNull.Value),
            new SqlParameter("@tel", (object?)proveedor.Telefono ?? DBNull.Value),
            new SqlParameter("@act", proveedor.Activo)
        };

        ExecuteNonQuery(sql, parameters, false);
    }

    public void Actualizar(ProveedorDTO proveedor, UsuarioDTO ejecutor)
    {
        if (!ejecutor.TienePermiso("compras") || ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Permisos insuficientes para modificar proveedores.");

        string sql = @"UPDATE PROVEEDOR 
                       SET identificacion = @ide, nombre = @nom, correo = @cor, telefono = @tel, activo = @act 
                       WHERE id = @id";

        SqlParameter[] parameters = {
            new SqlParameter("@id", proveedor.Id),
            new SqlParameter("@ide", proveedor.Identificacion),
            new SqlParameter("@nom", proveedor.Nombre),
            new SqlParameter("@cor", (object?)proveedor.Correo ?? DBNull.Value),
            new SqlParameter("@tel", (object?)proveedor.Telefono ?? DBNull.Value),
            new SqlParameter("@act", proveedor.Activo)
        };

        int filas = ExecuteNonQuery(sql, parameters, false);
        if (filas == 0) throw new Exception("El proveedor no existe.");
    }

    public void Eliminar(int id, UsuarioDTO ejecutor)
    {
        // Solo un administrador o alguien de compras puede "eliminar" (desactivar)
        if (!ejecutor.TienePermiso("compras") || ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("No tiene autoridad para eliminar proveedores.");

        string sql = "UPDATE PROVEEDOR SET activo = 0 WHERE id = @id";
        ExecuteNonQuery(sql, [new SqlParameter("@id", id)], false);
    }
}