using Microsoft.Data.SqlClient;
using IntegraPro.DTO.Models;
using System.Data;
using IntegraPro.DataAccess.Dao;
using System;
using System.Collections.Generic;

namespace IntegraPro.DataAccess.Factory;

public class ProveedorFactory(string connectionString) : MasterDao(connectionString)
{
    public List<ProveedorDTO> ObtenerTodos(UsuarioDTO ejecutor)
    {
        // SEGURIDAD: Validación directa usando el DTO para evitar dependencia de AppLogic
        if (!ejecutor.TienePermiso("proveedores"))
        {
            throw new UnauthorizedAccessException("No tiene permisos para acceder al módulo de proveedores.");
        }

        // Los proveedores suelen ser globales, no filtramos por sucursal_id
        string sql = "SELECT * FROM PROVEEDOR WHERE activo = 1";

        var dt = ExecuteQuery(sql, null, false);
        var lista = new List<ProveedorDTO>();

        foreach (DataRow row in dt.Rows)
        {
            lista.Add(MapearProveedor(row));
        }
        return lista;
    }

    public void Crear(ProveedorDTO proveedor, UsuarioDTO ejecutor)
    {
        // SEGURIDAD: Validación de acceso y escritura
        if (!ejecutor.TienePermiso("proveedores"))
            throw new UnauthorizedAccessException("No tiene permisos para el módulo de proveedores.");

        if (ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Su cuenta es de solo lectura. No puede realizar esta acción.");

        // TRAZABILIDAD: Guardamos quién lo creó 
        string sql = @"INSERT INTO PROVEEDOR (identificacion, nombre, correo, telefono, creado_por, activo) 
                       VALUES (@ide, @nom, @cor, @tel, @usr, 1)";

        SqlParameter[] parameters = {
            new SqlParameter("@ide", proveedor.Identificacion),
            new SqlParameter("@nom", proveedor.Nombre),
            new SqlParameter("@cor", (object?)proveedor.Correo ?? DBNull.Value),
            new SqlParameter("@tel", (object?)proveedor.Telefono ?? DBNull.Value),
            new SqlParameter("@usr", ejecutor.Id) // ID del usuario que registra
        };

        ExecuteNonQuery(sql, parameters, false);
    }

    public void Actualizar(ProveedorDTO proveedor, UsuarioDTO ejecutor)
    {
        // SEGURIDAD
        if (!ejecutor.TienePermiso("proveedores"))
            throw new UnauthorizedAccessException("No tiene permisos para el módulo de proveedores.");

        if (ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Su cuenta es de solo lectura.");

        string sql = @"UPDATE PROVEEDOR 
                       SET identificacion = @ide, nombre = @nom, correo = @cor, telefono = @tel 
                       WHERE id = @id";

        SqlParameter[] parameters = {
            new SqlParameter("@id", proveedor.Id),
            new SqlParameter("@ide", proveedor.Identificacion),
            new SqlParameter("@nom", proveedor.Nombre),
            new SqlParameter("@cor", (object?)proveedor.Correo ?? DBNull.Value),
            new SqlParameter("@tel", (object?)proveedor.Telefono ?? DBNull.Value)
        };

        int filasAfectadas = ExecuteNonQuery(sql, parameters, false);

        if (filasAfectadas == 0)
            throw new Exception("No se encontró el proveedor para actualizar.");
    }

    public void Eliminar(int id, UsuarioDTO ejecutor)
    {
        // SEGURIDAD
        if (!ejecutor.TienePermiso("proveedores"))
            throw new UnauthorizedAccessException("No tiene permisos para el módulo de proveedores.");

        if (ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Su cuenta es de solo lectura.");

        // Borrado lógico
        string sql = "UPDATE PROVEEDOR SET activo = 0 WHERE id = @id";
        ExecuteNonQuery(sql, [new SqlParameter("@id", id)], false);
    }

    private ProveedorDTO MapearProveedor(DataRow row)
    {
        return new ProveedorDTO
        {
            Id = (int)row["id"],
            Identificacion = row["identificacion"].ToString() ?? string.Empty,
            Nombre = row["nombre"].ToString() ?? string.Empty,
            Correo = row["correo"]?.ToString(),
            Telefono = row["telefono"]?.ToString(),
            Activo = row["activo"] != DBNull.Value && Convert.ToBoolean(row["activo"])
        };
    }
}