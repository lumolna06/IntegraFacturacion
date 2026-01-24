using IntegraPro.DTO.Models;
using IntegraPro.AppLogic.Utils;

namespace IntegraPro.AppLogic.Interfaces;

public interface IUsuarioService
{
    ApiResponse<UsuarioDTO> Login(string username, string password);
    ApiResponse<bool> Registrar(UsuarioDTO usuario);
    ApiResponse<bool> Logout(int usuarioId);
    ApiResponse<bool> ForzarCierreSesion(string username, string password);

    // ==========================================
    // MÉTODOS DE ADMINISTRACIÓN DE ROLES/USUARIOS
    // ==========================================
    ApiResponse<List<UsuarioDTO>> ObtenerTodos();
    ApiResponse<bool> ActualizarRol(int usuarioId, int nuevoRolId);
    ApiResponse<List<RolDTO>> ListarRolesDisponibles();
}