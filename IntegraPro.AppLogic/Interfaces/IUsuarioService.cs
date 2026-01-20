using IntegraPro.DTO.Models;
using IntegraPro.AppLogic.Utils;

namespace IntegraPro.AppLogic.Interfaces;

public interface IUsuarioService
{
    ApiResponse<UsuarioDTO> Login(string username, string password);
    ApiResponse<bool> Registrar(UsuarioDTO usuario);

    // Añadimos los métodos que faltaban
    ApiResponse<bool> Logout(int usuarioId);
    ApiResponse<bool> ForzarCierreSesion(string username, string password);
}