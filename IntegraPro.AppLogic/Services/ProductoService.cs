using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class ProductoService(ProductoFactory factory) : IProductoService
{
    private readonly ProductoFactory _factory = factory;

    public ApiResponse<List<ProductoDTO>> ObtenerTodos(UsuarioDTO ejecutor)
    {
        try
        {
            // SEGURIDAD: Validación preventiva de acceso al módulo
            ejecutor.ValidarAcceso("productos");

            var productos = _factory.GetAll(ejecutor);
            return new ApiResponse<List<ProductoDTO>>(true, "Productos obtenidos exitosamente.", productos);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<List<ProductoDTO>>(false, ex.Message); }
        catch (Exception ex) { return new ApiResponse<List<ProductoDTO>>(false, $"Error: {ex.Message}"); }
    }

    public ApiResponse<ProductoDTO> ObtenerPorId(int id, UsuarioDTO ejecutor)
    {
        try
        {
            ejecutor.ValidarAcceso("productos");

            var prod = _factory.GetById(id, ejecutor);
            return new ApiResponse<ProductoDTO>(prod != null,
                prod != null ? "Producto encontrado." : "El producto no existe o no tiene permisos para verlo.", prod);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<ProductoDTO>(false, ex.Message); }
        catch (Exception ex) { return new ApiResponse<ProductoDTO>(false, ex.Message); }
    }

    public ApiResponse<bool> Crear(ProductoDTO producto, UsuarioDTO ejecutor)
    {
        try
        {
            // SEGURIDAD: Validamos acceso y escritura antes de procesar nada
            ejecutor.ValidarAcceso("productos");
            ejecutor.ValidarEscritura();

            if (string.IsNullOrWhiteSpace(producto.Nombre))
                return new ApiResponse<bool>(false, "El nombre del producto es obligatorio.", false);

            // 1. Crear el producto base
            int nuevoId = _factory.Create(producto, ejecutor);

            // 2. Si es un producto elaborado, insertamos su receta/composición
            if (producto.EsElaborado && producto.Receta != null && producto.Receta.Any())
            {
                foreach (var item in producto.Receta)
                {
                    _factory.InsertarComposicion(nuevoId, item.MaterialId, item.CantidadNecesaria, ejecutor);
                }
            }

            return new ApiResponse<bool>(true, "Producto creado exitosamente.", true);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<bool>(false, ex.Message, false); }
        catch (Exception ex) { return new ApiResponse<bool>(false, $"Error al crear: {ex.Message}", false); }
    }

    public ApiResponse<bool> Actualizar(ProductoDTO producto, UsuarioDTO ejecutor)
    {
        try
        {
            ejecutor.ValidarAcceso("productos");
            ejecutor.ValidarEscritura();

            if (producto.Id <= 0)
                return new ApiResponse<bool>(false, "ID de producto no válido.", false);

            _factory.Update(producto, ejecutor);
            return new ApiResponse<bool>(true, "Producto actualizado correctamente.", true);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<bool>(false, ex.Message, false); }
        catch (Exception ex) { return new ApiResponse<bool>(false, $"Error al actualizar: {ex.Message}", false); }
    }

    public ApiResponse<List<ProductoDTO>> ObtenerAlertasStock(UsuarioDTO ejecutor)
    {
        try
        {
            ejecutor.ValidarAcceso("productos");

            var alertas = _factory.GetStockAlerts(ejecutor);
            return new ApiResponse<List<ProductoDTO>>(true, $"Se encontraron {alertas.Count} productos bajo el stock mínimo.", alertas);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<List<ProductoDTO>>(false, ex.Message); }
        catch (Exception ex) { return new ApiResponse<List<ProductoDTO>>(false, ex.Message); }
    }
}