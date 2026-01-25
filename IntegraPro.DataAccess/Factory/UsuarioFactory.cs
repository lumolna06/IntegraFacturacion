using System;
using System.Collections.Generic;
using System.Data;
using IntegraPro.DataAccess.Dao;
using IntegraPro.DataAccess.Mappers;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;

namespace IntegraPro.DataAccess.Factory;

public class UsuarioFactory : MasterDao
{
    private readonly UsuarioMapper _mapper;

    public UsuarioFactory(string connectionString) : base(connectionString)
    {
        _mapper = new UsuarioMapper();
    }

    /// <summary>
    /// Obtiene un usuario y sus permisos. Punto de entrada para Auth.
    /// Exento de validación de ejecutor.
    /// </summary>
    public UsuarioDTO? GetByUsername(string username)
    {
        var parameters = new SqlParameter[] {
            new SqlParameter("@username", username)
        };

        // sp_Usuario_GetByUsername retorna: id, rol_id, sucursal_id, nombre_completo, 
        // username, password_hash, correo_electronico, activo, nombre_rol, permisos_json
        var table = ExecuteQuery("sp_Usuario_GetByUsername", parameters);

        if (table == null || table.Rows.Count == 0) return null;

        return _mapper.MapFromRow(table.Rows[0]);
    }

    /// <summary>
    /// Crea un usuario validando permisos y jurisdicción de sucursal.
    /// </summary>
    public void Create(UsuarioDTO nuevoUsuario, UsuarioDTO ejecutor)
    {
        // SEGURIDAD: Validación directa usando el DTO
        if (!ejecutor.TienePermiso("usuarios"))
            throw new UnauthorizedAccessException("No tiene permisos para gestionar usuarios.");

        if (ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Su cuenta está en modo solo lectura.");

        // Lógica de Jurisdicción: Si el admin está limitado a su sucursal
        if (ejecutor.TienePermiso("sucursal_limit"))
        {
            nuevoUsuario.SucursalId = ejecutor.SucursalId;

            // Bloqueo de jerarquía: No puede crear Admins Globales (Rol 1)
            if (nuevoUsuario.RolId == 1)
                throw new UnauthorizedAccessException("No tiene autoridad para asignar el rol de Administrador Global.");
        }

        var parameters = new SqlParameter[] {
            new SqlParameter("@rol_id", nuevoUsuario.RolId),
            new SqlParameter("@sucursal_id", nuevoUsuario.SucursalId),
            new SqlParameter("@nombre_completo", nuevoUsuario.NombreCompleto),
            new SqlParameter("@username", nuevoUsuario.Username),
            new SqlParameter("@password_hash", nuevoUsuario.PasswordHash),
            new SqlParameter("@correo_electronico", (object?)nuevoUsuario.CorreoElectronico ?? DBNull.Value),
            new SqlParameter("@activo", nuevoUsuario.Activo)
        };

        ExecuteStoredProcedure("sp_Usuario_Insert", parameters);
    }

    /// <summary>
    /// Lista usuarios filtrando por sucursal automáticamente según el perfil del ejecutor.
    /// </summary>
    public List<UsuarioDTO> GetAll(UsuarioDTO ejecutor)
    {
        // SEGURIDAD: Solo usuarios autorizados listan usuarios
        if (!ejecutor.TienePermiso("usuarios"))
            return new List<UsuarioDTO>();

        bool limitarSucursal = ejecutor.TienePermiso("sucursal_limit");

        string sql = @"SELECT u.*, r.nombre_rol, r.permisos_json 
                       FROM USUARIO u 
                       INNER JOIN ROL r ON u.rol_id = r.id
                       WHERE 1=1";

        List<SqlParameter> parameters = new List<SqlParameter>();

        if (limitarSucursal)
        {
            sql += " AND u.sucursal_id = @sucursalId";
            parameters.Add(new SqlParameter("@sucursalId", ejecutor.SucursalId));
        }

        var table = ExecuteQuery(sql, parameters.ToArray(), false);
        var lista = new List<UsuarioDTO>();

        if (table != null)
        {
            foreach (DataRow row in table.Rows)
            {
                lista.Add(_mapper.MapFromRow(row));
            }
        }

        return lista;
    }

    /// <summary>
    /// Actualiza el rol verificando jurisdicción de sucursal.
    /// </summary>
    public void ActualizarRol(int usuarioId, int nuevoRolId, UsuarioDTO ejecutor)
    {
        if (!ejecutor.TienePermiso("usuarios") || ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Permisos insuficientes para modificar roles.");

        // Bloqueo de escalada de privilegios
        if (ejecutor.TienePermiso("sucursal_limit") && nuevoRolId == 1)
            throw new UnauthorizedAccessException("No puede asignar administración global desde una cuenta limitada por sucursal.");

        string sql = "UPDATE USUARIO SET rol_id = @rolId WHERE id = @id";
        var parameters = new List<SqlParameter> {
            new SqlParameter("@id", usuarioId),
            new SqlParameter("@rolId", nuevoRolId)
        };

        // Forzamos que solo pueda editar usuarios de SU sucursal si tiene el límite
        if (ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " AND sucursal_id = @sid";
            parameters.Add(new SqlParameter("@sid", ejecutor.SucursalId));
        }

        int filas = ExecuteNonQuery(sql, parameters.ToArray(), false);

        if (filas == 0)
            throw new Exception("No se pudo actualizar: El usuario no existe o no pertenece a su sucursal.");
    }

    public List<RolDTO> GetRoles(UsuarioDTO ejecutor)
    {
        if (!ejecutor.TienePermiso("usuarios")) return new List<RolDTO>();

        string sql = "SELECT id, nombre_rol, permisos_json FROM ROL";

        // Un admin de sucursal no debe ver ni asignar el rol de Admin Global (ID 1)
        if (ejecutor.TienePermiso("sucursal_limit"))
            sql += " WHERE id > 1";

        var table = ExecuteQuery(sql, [], false);
        var lista = new List<RolDTO>();

        if (table != null)
        {
            foreach (DataRow row in table.Rows)
            {
                lista.Add(new RolDTO
                {
                    Id = Convert.ToInt32(row["id"]),
                    NombreRol = row["nombre_rol"].ToString() ?? "",
                    PermisosJson = row["permisos_json"].ToString() ?? "{}"
                });
            }
        }
        return lista;
    }

    public void ActualizarSesionHardware(int usuarioId, string? hardwareId)
    {
        var parameters = new SqlParameter[] {
            new SqlParameter("@id", usuarioId),
            new SqlParameter("@hardware_id_sesion", (object?)hardwareId ?? DBNull.Value)
        };
        ExecuteNonQuery("sp_Usuario_ActualizarSesion", parameters);
    }

    public void RegistrarLogin(int usuarioId)
    {
        var parameters = new SqlParameter[] {
            new SqlParameter("@id", usuarioId)
        };
        ExecuteNonQuery("sp_Usuario_RegistrarLogin", parameters);
    }
}