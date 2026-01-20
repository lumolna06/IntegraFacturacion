using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Interfaces;

public interface IProductoService
{
    ApiResponse<List<ProductoDTO>> ObtenerTodos();
    ApiResponse<ProductoDTO> ObtenerPorId(int id);
    ApiResponse<bool> Crear(ProductoDTO producto);
    ApiResponse<bool> Actualizar(ProductoDTO producto);
    ApiResponse<List<ProductoDTO>> ObtenerAlertasStock();
}