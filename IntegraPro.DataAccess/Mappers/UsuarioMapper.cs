using System.Data;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;

namespace IntegraPro.DataAccess.Mappers;

public class UsuarioMapper : IMapper<UsuarioDTO>
{
    public UsuarioDTO MapFromRow(DataRow row)
    {
        var columns = row.Table.Columns;

        return new UsuarioDTO
        {
            Id = columns.Contains("id") ? Convert.ToInt32(row["id"]) : 0,
            RolId = columns.Contains("rol_id") ? Convert.ToInt32(row["rol_id"]) : 0,
            SucursalId = columns.Contains("sucursal_id") ? Convert.ToInt32(row["sucursal_id"]) : 0,
            NombreCompleto = columns.Contains("nombre_completo") ? row["nombre_completo"]?.ToString() ?? "" : "",
            Username = columns.Contains("username") ? row["username"]?.ToString() ?? "" : "",

            // Aquí estaba el error: Validación de existencia para el Hash
            PasswordHash = columns.Contains("password_hash") ? row["password_hash"]?.ToString() ?? "" : "",

            CorreoElectronico = columns.Contains("correo_electronico") && row["correo_electronico"] != DBNull.Value
                                ? row["correo_electronico"].ToString() ?? "" : "",

            NombreRol = columns.Contains("nombre_rol") && row["nombre_rol"] != DBNull.Value
                        ? row["nombre_rol"].ToString() : null,

            PermisosJson = columns.Contains("permisos_json") && row["permisos_json"] != DBNull.Value
                           ? row["permisos_json"].ToString() : null,

            Activo = columns.Contains("activo") && row["activo"] != DBNull.Value && Convert.ToBoolean(row["activo"]),

            UltimoLogin = columns.Contains("ultimo_login") && row["ultimo_login"] != DBNull.Value
                          ? Convert.ToDateTime(row["ultimo_login"]) : null,

            HardwareIdSesion = columns.Contains("hardware_id_sesion") && row["hardware_id_sesion"] != DBNull.Value
                               ? row["hardware_id_sesion"].ToString() : null
        };
    }

    public SqlParameter[] MapToParameters(UsuarioDTO entity)
    {
        return new SqlParameter[]
        {
            new SqlParameter("@id", entity.Id),
            new SqlParameter("@rol_id", entity.RolId),
            new SqlParameter("@sucursal_id", entity.SucursalId),
            new SqlParameter("@nombre_completo", entity.NombreCompleto ?? (object)DBNull.Value),
            new SqlParameter("@username", entity.Username ?? (object)DBNull.Value),
            new SqlParameter("@password_hash", entity.PasswordHash ?? (object)DBNull.Value),
            new SqlParameter("@correo_electronico", entity.CorreoElectronico ?? (object)DBNull.Value), // Añadido para INSERT/UPDATE
            new SqlParameter("@activo", entity.Activo),
            new SqlParameter("@hardware_id_sesion", (object?)entity.HardwareIdSesion ?? DBNull.Value)
        };
    }
}