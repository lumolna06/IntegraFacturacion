using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Interfaces;

public interface IProductoService
{
    /// <summary>
    /// Lista los productos activos. Filtra automáticamente por la sucursal del ejecutor 
    /// si este tiene el permiso 'sucursal_limit'.
    /// </summary>
    ApiResponse<List<ProductoDTO>> ObtenerTodos(UsuarioDTO ejecutor);

    /// <summary>
    /// Obtiene un producto específico. Valida que pertenezca a la sucursal permitida.
    /// </summary>
    ApiResponse<ProductoDTO> ObtenerPorId(int id, UsuarioDTO ejecutor);

    /// <summary>
    /// Registra un nuevo producto y su receta (si es elaborado). 
    /// Requiere permiso de escritura y asocia la sucursal según el perfil.
    /// </summary>
    ApiResponse<bool> Crear(ProductoDTO producto, UsuarioDTO ejecutor);

    /// <summary>
    /// Actualiza datos maestros del producto. Valida pertenencia de sucursal.
    /// </summary>
    ApiResponse<bool> Actualizar(ProductoDTO producto, UsuarioDTO ejecutor);

    /// <summary>
    /// Consulta productos cuya existencia esté por debajo del stock mínimo configurado.
    /// </summary>
    ApiResponse<List<ProductoDTO>> ObtenerAlertasStock(UsuarioDTO ejecutor);
}