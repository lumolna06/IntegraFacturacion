using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class ProductoService(ProductoFactory factory) : IProductoService
{
    public ApiResponse<List<ProductoDTO>> ObtenerTodos() =>
        new(true, "Productos obtenidos", factory.GetAll());

    public ApiResponse<ProductoDTO> ObtenerPorId(int id)
    {
        var prod = factory.GetById(id);
        return new ApiResponse<ProductoDTO>(prod != null, prod != null ? "Encontrado" : "No existe", prod!);
    }

    public ApiResponse<bool> Crear(ProductoDTO producto)
    {
        try
        {
            int nuevoId = factory.Create(producto);
            if (producto.EsElaborado && producto.Receta != null)
            {
                foreach (var item in producto.Receta)
                    factory.InsertarComposicion(nuevoId, item.MaterialId, item.CantidadNecesaria);
            }
            return new ApiResponse<bool>(true, "Creado exitosamente", true);
        }
        catch (Exception ex) { return new ApiResponse<bool>(false, ex.Message, false); }
    }

    public ApiResponse<bool> Actualizar(ProductoDTO producto)
    {
        try
        {
            factory.Update(producto);
            return new ApiResponse<bool>(true, "Actualizado exitosamente", true);
        }
        catch (Exception ex) { return new ApiResponse<bool>(false, ex.Message, false); }
    }

    public ApiResponse<List<ProductoDTO>> ObtenerAlertasStock() =>
        new(true, "Alertas de stock", factory.GetStockAlerts());
}