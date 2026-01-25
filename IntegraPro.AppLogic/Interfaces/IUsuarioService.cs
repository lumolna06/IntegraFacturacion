using IntegraPro.DTO.Models;
using IntegraPro.AppLogic.Utils;

namespace IntegraPro.AppLogic.Interfaces;

public interface IUsuarioService
{
    ApiResponse<UsuarioDTO> Login(string username, string password);
    ApiResponse<bool> Registrar(UsuarioDTO usuario, UsuarioDTO ejecutor);
    ApiResponse<bool> Logout(int usuarioId);
    ApiResponse<bool> ForzarCierreSesion(string username, string password);
    ApiResponse<List<UsuarioDTO>> ObtenerTodos(UsuarioDTO ejecutor);
    ApiResponse<bool> ActualizarRol(int usuarioId, int nuevoRolId, UsuarioDTO ejecutor);
    ApiResponse<List<RolDTO>> ListarRolesDisponibles(UsuarioDTO ejecutor);
}