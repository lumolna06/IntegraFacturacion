using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Interfaces;

public interface IProveedorService
{
    ApiResponse<List<ProveedorDTO>> ObtenerTodos(UsuarioDTO ejecutor);
    ApiResponse<bool> Crear(ProveedorDTO proveedor, UsuarioDTO ejecutor);
    ApiResponse<bool> Actualizar(ProveedorDTO proveedor, UsuarioDTO ejecutor);
    ApiResponse<bool> Eliminar(int id, UsuarioDTO ejecutor);
}