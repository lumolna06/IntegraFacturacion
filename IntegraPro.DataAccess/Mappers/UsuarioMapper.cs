using System.Data;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;

namespace IntegraPro.DataAccess.Mappers;

public class UsuarioMapper : IMapper<UsuarioDTO>
{
    public UsuarioDTO MapFromRow(DataRow row)
    {
        return new UsuarioDTO
        {
            Id = Convert.ToInt32(row["id"]),
            RolId = Convert.ToInt32(row["rol_id"]),
            SucursalId = Convert.ToInt32(row["sucursal_id"]),
            NombreCompleto = row["nombre_completo"]?.ToString() ?? "",
            Username = row["username"]?.ToString() ?? "",
            PasswordHash = row["password_hash"]?.ToString() ?? "",
            Activo = row["activo"] != DBNull.Value && Convert.ToBoolean(row["activo"]),
            UltimoLogin = row["ultimo_login"] != DBNull.Value ? Convert.ToDateTime(row["ultimo_login"]) : null,
            HardwareIdSesion = row.Table.Columns.Contains("hardware_id_sesion") && row["hardware_id_sesion"] != DBNull.Value
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
            new SqlParameter("@activo", entity.Activo),
            new SqlParameter("@hardware_id_sesion", (object?)entity.HardwareIdSesion ?? DBNull.Value)
        };
    }
}