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
    /// Obtiene un usuario por su nombre de usuario. 
    /// Utilizado por el Login y por el LicenseFilter para validar la sesión activa.
    /// </summary>
    public UsuarioDTO? GetByUsername(string username)
    {
        var parameters = new SqlParameter[] {
            new SqlParameter("@username", username)
        };

        var table = ExecuteQuery("sp_Usuario_GetByUsername", parameters);

        if (table.Rows.Count == 0) return null;

        return _mapper.MapFromRow(table.Rows[0]);
    }

    public void Create(UsuarioDTO usuario)
    {
        // Definimos exactamente los 7 parámetros que espera tu SP
        var parameters = new SqlParameter[] {
        new SqlParameter("@id", 0), // El SP lo pide pero no lo usa
        new SqlParameter("@rol_id", usuario.RolId),
        new SqlParameter("@sucursal_id", usuario.SucursalId),
        new SqlParameter("@nombre_completo", usuario.NombreCompleto),
        new SqlParameter("@username", usuario.Username),
        new SqlParameter("@password_hash", usuario.PasswordHash),
        new SqlParameter("@activo", usuario.Activo)
    };

        // Ejecutamos el procedimiento
        ExecuteStoredProcedure("sp_Usuario_Insert", parameters);
    }

    /// <summary>
    /// Registra o limpia el Hardware ID asociado a la sesión activa del usuario.
    /// </summary>
    /// <param name="usuarioId">ID del usuario en la base de datos.</param>
    /// <param name="hardwareId">ID de la PC. Si es null, libera la sesión (Logout).</param>
    public void ActualizarSesionHardware(int usuarioId, string? hardwareId)
    {
        var parameters = new SqlParameter[] {
            new SqlParameter("@id", usuarioId),
            // Si hardwareId es null, enviamos DBNull.Value a SQL para limpiar el campo
            new SqlParameter("@hardware_id_sesion", (object?)hardwareId ?? DBNull.Value)
        };

        // Ejecuta el procedimiento almacenado que actualiza la tabla USUARIO
        ExecuteNonQuery("sp_Usuario_ActualizarSesion", parameters);
    }

    public void RegistrarLogin(int usuarioId)
    {
        var parameters = new Microsoft.Data.SqlClient.SqlParameter[] {
        new Microsoft.Data.SqlClient.SqlParameter("@id", usuarioId)
    };
        ExecuteNonQuery("sp_Usuario_RegistrarLogin", parameters);
    }

}