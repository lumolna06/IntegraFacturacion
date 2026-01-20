using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class InventarioService(InventarioFactory factory, ProductoFactory productoFactory) : IInventarioService
{
    public ApiResponse<bool> Registrar(MovimientoInventarioDTO mov)
    {
        try
        {
            if (mov.Cantidad <= 0)
                return new ApiResponse<bool>(false, "La cantidad debe ser mayor a cero", false);

            bool ok = factory.Insertar(mov);
            return new ApiResponse<bool>(ok, ok ? "Movimiento registrado y stock actualizado" : "Error al registrar", ok);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    public ApiResponse<bool> ProcesarProduccion(int productoPadreId, decimal cantidadAProducir, int usuarioId)
    {
        try
        {
            if (cantidadAProducir <= 0)
                return new ApiResponse<bool>(false, "La cantidad a producir debe ser mayor a cero", false);

            // 1. Obtener la receta del producto padre
            var receta = productoFactory.ObtenerComposicion(productoPadreId);

            if (receta == null || receta.Count == 0)
                return new ApiResponse<bool>(false, "El producto seleccionado no tiene una receta definida o no es un producto elaborado.", false);

            // 2. Registrar SALIDAS de materias primas (Explosión de materiales)
            foreach (var item in receta)
            {
                var movSalida = new MovimientoInventarioDTO
                {
                    ProductoId = item.MaterialId,
                    UsuarioId = usuarioId,
                    TipoMovimiento = "SALIDA",
                    Cantidad = item.CantidadNecesaria * cantidadAProducir,
                    DocumentoReferencia = $"PROD-P{productoPadreId}",
                    Notas = $"Consumo automático para producir {cantidadAProducir} unidades del producto padre ID {productoPadreId}"
                };

                if (!factory.Insertar(movSalida))
                    throw new Exception($"Error al rebajar stock del material ID {item.MaterialId}");
            }

            // 3. Registrar ENTRADA del producto terminado
            var movEntrada = new MovimientoInventarioDTO
            {
                ProductoId = productoPadreId,
                UsuarioId = usuarioId,
                TipoMovimiento = "ENTRADA",
                Cantidad = cantidadAProducir,
                DocumentoReferencia = "ORDEN-PROD",
                Notas = $"Ingreso por finalización de proceso productivo"
            };

            if (!factory.Insertar(movEntrada))
                throw new Exception("Error al ingresar el stock del producto terminado.");

            return new ApiResponse<bool>(true, $"Producción exitosa: Se descontaron los insumos y se cargaron {cantidadAProducir} unidades al inventario.", true);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, "Error en proceso de producción: " + ex.Message, false);
        }
    }
}