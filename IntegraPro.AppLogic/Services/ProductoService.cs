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
            var productos = _factory.GetAll(ejecutor);
            return new ApiResponse<List<ProductoDTO>>(true, "Productos obtenidos", productos);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<ProductoDTO>>(false, $"Error al obtener productos: {ex.Message}");
        }
    }

    public ApiResponse<ProductoDTO> ObtenerPorId(int id, UsuarioDTO ejecutor)
    {
        try
        {
            var prod = _factory.GetById(id, ejecutor);
            return new ApiResponse<ProductoDTO>(prod != null, prod != null ? "Encontrado" : "No existe o no tiene acceso", prod);
        }
        catch (Exception ex)
        {
            return new ApiResponse<ProductoDTO>(false, ex.Message);
        }
    }

    public ApiResponse<bool> Crear(ProductoDTO producto, UsuarioDTO ejecutor)
    {
        try
        {
            // Validaciones básicas de negocio
            if (string.IsNullOrWhiteSpace(producto.Nombre))
                return new ApiResponse<bool>(false, "El nombre del producto es obligatorio.", false);

            // El Factory se encarga de validar 'solo_lectura' y 'sucursal_limit'
            int nuevoId = _factory.Create(producto, ejecutor);

            // Si es un producto elaborado (combo o receta), insertamos su composición
            if (producto.EsElaborado && producto.Receta != null)
            {
                foreach (var item in producto.Receta)
                {
                    _factory.InsertarComposicion(nuevoId, item.MaterialId, item.CantidadNecesaria, ejecutor);
                }
            }

            return new ApiResponse<bool>(true, "Creado exitosamente", true);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<bool>(false, ex.Message, false);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, $"Error al crear producto: {ex.Message}", false);
        }
    }

    public ApiResponse<bool> Actualizar(ProductoDTO producto, UsuarioDTO ejecutor)
    {
        try
        {
            if (producto.Id <= 0)
                return new ApiResponse<bool>(false, "ID de producto no válido.", false);

            _factory.Update(producto, ejecutor);
            return new ApiResponse<bool>(true, "Actualizado exitosamente", true);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<bool>(false, ex.Message, false);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, $"Error al actualizar: {ex.Message}", false);
        }
    }

    public ApiResponse<List<ProductoDTO>> ObtenerAlertasStock(UsuarioDTO ejecutor)
    {
        try
        {
            var alertas = _factory.GetStockAlerts(ejecutor);
            return new ApiResponse<List<ProductoDTO>>(true, "Alertas de stock obtenidas", alertas);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<ProductoDTO>>(false, ex.Message);
        }
    }
}