using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Interfaces;

public interface IClienteService
{
    ApiResponse<List<ClienteDTO>> ObtenerTodos(UsuarioDTO ejecutor); // El service recibe ejecutor por estándar
    ApiResponse<int> Crear(ClienteDTO cliente, UsuarioDTO ejecutor); // Cambiado a int para el ID
    ApiResponse<bool> Actualizar(ClienteDTO cliente, UsuarioDTO ejecutor);
    ApiResponse<ClienteDTO> BuscarPorIdentificacion(string identificacion);
}