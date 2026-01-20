using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class InventarioService(InventarioFactory factory) : IInventarioService
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
}