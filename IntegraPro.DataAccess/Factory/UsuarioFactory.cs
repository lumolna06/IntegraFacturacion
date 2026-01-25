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

    // Ajustado para pasar la cadena de conexión al MasterDao base
    public UsuarioFactory(string connectionString) : base(connectionString)
    {
        _mapper = new UsuarioMapper();
    }

    /// <summary>
    /// Obtiene un usuario y sus permisos. Método exento de validación de ejecutor 
    /// porque es el punto de entrada para autenticación.
    /// </summary>
    public UsuarioDTO? GetByUsername(string username)
    {
        var parameters = new SqlParameter[] {
            new SqlParameter("@username", username)
        };

        // sp_Usuario_GetByUsername debe retornar: id, rol_id, sucursal_id, nombre_completo, 
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
        if (!ejecutor.TienePermiso("usuarios"))
            throw new UnauthorizedAccessException("No tiene permisos para gestionar usuarios.");

        if (ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Su cuenta está en modo solo lectura.");

        // Si el admin está limitado a su sucursal, forzamos que el nuevo usuario sea de esa sucursal
        if (ejecutor.TienePermiso("sucursal_limit"))
        {
            nuevoUsuario.SucursalId = ejecutor.SucursalId;

            // No permitimos que cree roles de mayor jerarquía (asumiendo 1 = Admin Global)
            if (nuevoUsuario.RolId == 1)
                throw new UnauthorizedAccessException("No tiene autoridad para asignar el rol de Administrador Global.");
        }

        var parameters = new SqlParameter[] {
            new SqlParameter("@id", 0),
            new SqlParameter("@rol_id", nuevoUsuario.RolId),
            new SqlParameter("@sucursal_id", nuevoUsuario.SucursalId),
            new SqlParameter("@nombre_completo", nuevoUsuario.NombreCompleto),
            new SqlParameter("@username", nuevoUsuario.Username),
            new SqlParameter("@password_hash", nuevoUsuario.PasswordHash),
            new SqlParameter("@correo_electronico", nuevoUsuario.CorreoElectronico ?? (object)DBNull.Value),
            new SqlParameter("@activo", nuevoUsuario.Activo)
        };

        ExecuteStoredProcedure("sp_Usuario_Insert", parameters);
    }

    /// <summary>
    /// Lista usuarios. Filtra por sucursal automáticamente según el perfil del ejecutor.
    /// </summary>
    public List<UsuarioDTO> GetAll(UsuarioDTO ejecutor)
    {
        int? sucursalFiltro = ejecutor.TienePermiso("sucursal_limit") ? ejecutor.SucursalId : null;

        string sql = @"SELECT u.*, r.nombre_rol, r.permisos_json 
                       FROM USUARIO u 
                       INNER JOIN ROL r ON u.rol_id = r.id
                       WHERE 1=1";

        List<SqlParameter> parameters = new List<SqlParameter>();

        if (sucursalFiltro.HasValue)
        {
            sql += " AND u.sucursal_id = @sucursalId";
            parameters.Add(new SqlParameter("@sucursalId", sucursalFiltro.Value));
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
    /// Actualiza el rol verificando que el usuario pertenezca a la sucursal del ejecutor si hay restricciones.
    /// </summary>
    public void ActualizarRol(int usuarioId, int nuevoRolId, UsuarioDTO ejecutor)
    {
        if (!ejecutor.TienePermiso("usuarios") || ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Permisos insuficientes para modificar roles.");

        if (ejecutor.TienePermiso("sucursal_limit") && nuevoRolId == 1)
            throw new UnauthorizedAccessException("Jurisdicción insuficiente para asignar administración global.");

        string sql = "UPDATE USUARIO SET rol_id = @rolId WHERE id = @id";
        var parameters = new List<SqlParameter> {
            new SqlParameter("@id", usuarioId),
            new SqlParameter("@rolId", nuevoRolId)
        };

        if (ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " AND sucursal_id = @sid";
            parameters.Add(new SqlParameter("@sid", ejecutor.SucursalId));
        }

        int filas = ExecuteNonQuery(sql, parameters.ToArray(), false);

        if (filas == 0)
            throw new Exception("Operación fallida: El usuario no existe o se encuentra fuera de su sucursal.");
    }

    public List<RolDTO> GetRoles(UsuarioDTO ejecutor)
    {
        if (!ejecutor.TienePermiso("usuarios")) return new List<RolDTO>();

        string sql = "SELECT id, nombre_rol, permisos_json FROM ROL";

        // El admin de sucursal no debe ver/asignar el rol de Admin Global (ID 1)
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