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

    public UsuarioDTO? GetByUsername(string username)
    {
        var parameters = new SqlParameter[] {
            new SqlParameter("@username", username)
        };

        // NOTA: Para que nombre_rol y permisos_json no lleguen nulos en el login,
        // el SP 'sp_Usuario_GetByUsername' debe hacer el INNER JOIN con la tabla ROL.
        var table = ExecuteQuery("sp_Usuario_GetByUsername", parameters);

        if (table.Rows.Count == 0) return null;

        return _mapper.MapFromRow(table.Rows[0]);
    }

    public void Create(UsuarioDTO usuario)
    {
        // Agregamos el correo_electronico a los parámetros del insert
        var parameters = new SqlParameter[] {
            new SqlParameter("@id", 0),
            new SqlParameter("@rol_id", usuario.RolId),
            new SqlParameter("@sucursal_id", usuario.SucursalId),
            new SqlParameter("@nombre_completo", usuario.NombreCompleto),
            new SqlParameter("@username", usuario.Username),
            new SqlParameter("@password_hash", usuario.PasswordHash),
            new SqlParameter("@correo_electronico", usuario.CorreoElectronico ?? (object)DBNull.Value), // NUEVO
            new SqlParameter("@activo", usuario.Activo)
        };

        ExecuteStoredProcedure("sp_Usuario_Insert", parameters);
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

    // ==========================================
    // NUEVOS MÉTODOS PARA GESTIÓN DE ROLES
    // ==========================================

    public List<UsuarioDTO> GetAll()
    {
        // Modificamos u.* por u.correo_electronico explícito para asegurar que el Mapper lo encuentre
        string sql = @"SELECT u.id, u.rol_id, u.sucursal_id, u.nombre_completo, u.username, 
                              u.correo_electronico, u.activo, u.ultimo_login, u.hardware_id_sesion,
                              r.nombre_rol, r.permisos_json 
                       FROM USUARIO u 
                       INNER JOIN ROL r ON u.rol_id = r.id";

        var table = ExecuteQuery(sql, [], false);
        var lista = new List<UsuarioDTO>();

        foreach (DataRow row in table.Rows)
        {
            lista.Add(_mapper.MapFromRow(row));
        }

        return lista;
    }

    public void ActualizarRol(int usuarioId, int nuevoRolId)
    {
        string sql = "UPDATE USUARIO SET rol_id = @rolId WHERE id = @id";
        var parameters = new SqlParameter[] {
            new SqlParameter("@id", usuarioId),
            new SqlParameter("@rolId", nuevoRolId)
        };

        ExecuteNonQuery(sql, parameters, false);
    }

    public List<RolDTO> GetRoles()
    {
        string sql = "SELECT id, nombre_rol, permisos_json FROM ROL";
        var table = ExecuteQuery(sql, [], false);
        var lista = new List<RolDTO>();

        foreach (DataRow row in table.Rows)
        {
            lista.Add(new RolDTO
            {
                Id = Convert.ToInt32(row["id"]),
                NombreRol = row["nombre_rol"].ToString() ?? "",
                PermisosJson = row["permisos_json"].ToString() ?? "{}"
            });
        }

        return lista;
    }
}