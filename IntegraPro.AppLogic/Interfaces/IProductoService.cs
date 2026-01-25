using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Interfaces;

public interface IProductoService
{
    // Todos los métodos ahora requieren el 'ejecutor' para filtrar por sucursal o validar roles
    ApiResponse<List<ProductoDTO>> ObtenerTodos(UsuarioDTO ejecutor);

    ApiResponse<ProductoDTO> ObtenerPorId(int id, UsuarioDTO ejecutor);

    ApiResponse<bool> Crear(ProductoDTO producto, UsuarioDTO ejecutor);

    ApiResponse<bool> Actualizar(ProductoDTO producto, UsuarioDTO ejecutor);

    ApiResponse<List<ProductoDTO>> ObtenerAlertasStock(UsuarioDTO ejecutor);
}