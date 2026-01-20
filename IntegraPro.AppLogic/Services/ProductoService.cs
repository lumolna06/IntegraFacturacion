using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class ProductoService : IProductoService
{
    private readonly ProductoFactory _factory;

    public ProductoService(ProductoFactory factory)
    {
        _factory = factory;
    }

    public ApiResponse<List<ProductoDTO>> ObtenerTodos()
    {
        try
        {
            var lista = _factory.GetAll();
            return new ApiResponse<List<ProductoDTO>>(true, "Productos recuperados con éxito", lista);
        }
        catch (Exception ex)
        {
            Logger.WriteLog("Error", "ObtenerTodosProductos", ex.Message);
            return new ApiResponse<List<ProductoDTO>>(false, $"Error al obtener productos: {ex.Message}");
        }
    }

    public ApiResponse<bool> Crear(ProductoDTO producto)
    {
        try
        {
            // Validaciones básicas
            Guard.AgainstEmptyString(producto.Nombre, "Nombre del Producto");
            if (producto.Precio1 < 0) return new ApiResponse<bool>(false, "El precio no puede ser negativo");

            // 1. Insertar el producto en la tabla PRODUCTO
            // El factory debe devolver el ID generado (SCOPE_IDENTITY)
            int nuevoProductoId = _factory.Create(producto);

            // 2. Si el producto es elaborado (tiene receta), guardamos su composición
            if (producto.EsElaborado && producto.Receta != null && producto.Receta.Any())
            {
                foreach (var item in producto.Receta)
                {
                    // Guardamos cada material asociado al producto padre
                    _factory.InsertarComposicion(nuevoProductoId, item.MaterialId, item.CantidadNecesaria);
                }
                Logger.WriteLog("Inventario", "CrearProducto", $"Producto elaborado '{producto.Nombre}' creado con {producto.Receta.Count} materiales.");
            }
            else
            {
                Logger.WriteLog("Inventario", "CrearProducto", $"Producto simple '{producto.Nombre}' creado.");
            }

            return new ApiResponse<bool>(true, "Producto registrado correctamente", true);
        }
        catch (Exception ex)
        {
            Logger.WriteLog("Error", "CrearProducto", ex.Message);
            return new ApiResponse<bool>(false, $"Error al crear producto: {ex.Message}");
        }
    }

    public ApiResponse<List<ProductoDTO>> ObtenerAlertasStock()
    {
        try
        {
            // Utiliza la vista VW_ALERTA_STOCK_BAJO definida en tu SQL
            var alertas = _factory.GetStockAlerts();
            return new ApiResponse<List<ProductoDTO>>(true, "Alertas de stock bajo obtenidas", alertas);
        }
        catch (Exception ex)
        {
            Logger.WriteLog("Error", "ObtenerAlertasStock", ex.Message);
            return new ApiResponse<List<ProductoDTO>>(false, ex.Message);
        }
    }

    public ApiResponse<ProductoDTO> ObtenerPorId(int id)
    {
        try
        {
            var producto = _factory.GetById(id);
            if (producto == null) return new ApiResponse<ProductoDTO>(false, "Producto no encontrado");
            return new ApiResponse<ProductoDTO>(true, "Producto encontrado", producto);
        }
        catch (Exception ex)
        {
            return new ApiResponse<ProductoDTO>(false, ex.Message);
        }
    }

    public ApiResponse<bool> Actualizar(ProductoDTO producto)
    {
        try
        {
            _factory.Update(producto);
            return new ApiResponse<bool>(true, "Producto actualizado con éxito", true);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, ex.Message);
        }
    }
}