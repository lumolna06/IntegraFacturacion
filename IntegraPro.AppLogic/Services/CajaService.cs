using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class CajaService(CajaFactory factory) : ICajaService
{
    private readonly CajaFactory _factory = factory;

    public ApiResponse<int> AbrirCaja(CajaAperturaDTO apertura, UsuarioDTO ejecutor)
    {
        try
        {
            // --- SEGURIDAD: Validación preventiva ---
            ejecutor.ValidarAcceso("caja");
            ejecutor.ValidarEscritura();

            int id = _factory.AbrirCaja(apertura, ejecutor);
            return new ApiResponse<int>(true, "Caja abierta con éxito", id);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<int>(false, ex.Message);
        }
        catch (Exception ex)
        {
            return new ApiResponse<int>(false, $"Error al abrir caja: {ex.Message}");
        }
    }

    public ApiResponse<bool> CerrarCaja(CajaCierreDTO cierre, UsuarioDTO ejecutor)
    {
        try
        {
            // --- SEGURIDAD: Validación preventiva ---
            ejecutor.ValidarAcceso("caja");
            ejecutor.ValidarEscritura();

            _factory.CerrarCaja(cierre, ejecutor);
            return new ApiResponse<bool>(true, "Caja cerrada correctamente", true);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<bool>(false, ex.Message, false);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    public ApiResponse<List<object>> ObtenerHistorial(UsuarioDTO ejecutor)
    {
        try
        {
            // --- SEGURIDAD: Validación preventiva ---
            ejecutor.ValidarAcceso("caja");

            var historial = _factory.ObtenerHistorialCierres(ejecutor);
            return new ApiResponse<List<object>>(true, "Historial recuperado", historial);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<List<object>>(false, ex.Message);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<object>>(false, $"Error: {ex.Message}");
        }
    }
}