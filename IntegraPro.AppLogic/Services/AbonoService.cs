using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class AbonoService(AbonoFactory factory) : IAbonoService
{
    private readonly AbonoFactory _factory = factory;

    // 1. PROCESAR ABONO
    public ApiResponse<bool> ProcesarAbono(AbonoDTO abono, int clienteId, UsuarioDTO ejecutor)
    {
        try
        {
            // --- SEGURIDAD: Validar antes de tocar la DB ---
            ejecutor.ValidarAcceso("abonos");
            ejecutor.ValidarEscritura();

            // Validaciones básicas de negocio
            if (abono.MontoAbonado <= 0)
                return new ApiResponse<bool>(false, "El monto del abono debe ser mayor a cero.", false);

            _factory.ProcesarAbonoCompleto(abono, clienteId, ejecutor);

            return new ApiResponse<bool>(true, "Abono procesado y saldos actualizados", true);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<bool>(false, ex.Message, false);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, $"Error en la operación: {ex.Message}", false);
        }
    }

    // 2. BUSCAR CUENTAS
    public ApiResponse<List<CxcConsultaDTO>> BuscarCuentas(string filtro, UsuarioDTO ejecutor)
    {
        try
        {
            ejecutor.ValidarAcceso("abonos");

            var datos = _factory.BuscarCxcClientes(filtro, ejecutor);
            return new ApiResponse<List<CxcConsultaDTO>>(true, "Cuentas localizadas", datos);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<List<CxcConsultaDTO>>(false, ex.Message);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<CxcConsultaDTO>>(false, ex.Message);
        }
    }

    // ACTUALIZADO: Ahora recibe UsuarioDTO para validar acceso
    public ApiResponse<List<AbonoHistorialDTO>> ObtenerHistorial(int facturaId, UsuarioDTO ejecutor)
    {
        try
        {
            // Seguridad: Validamos que el usuario pueda ver el módulo de abonos
            ejecutor.ValidarAcceso("abonos");

            var lista = _factory.ListarHistorialAbonos(facturaId);
            return new ApiResponse<List<AbonoHistorialDTO>>(true, "Historial cargado", lista);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<List<AbonoHistorialDTO>>(false, ex.Message);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<AbonoHistorialDTO>>(false, $"Error al cargar historial: {ex.Message}");
        }
    }

    // 3. ALERTAS DE MORA
    public ApiResponse<List<AlertaCxcDTO>> ObtenerAlertasMora(UsuarioDTO ejecutor)
    {
        try
        {
            ejecutor.ValidarAcceso("abonos");
            var alertas = _factory.ObtenerAlertasVencimiento(ejecutor);
            return new ApiResponse<List<AlertaCxcDTO>>(true, "Alertas de mora obtenidas", alertas);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<List<AlertaCxcDTO>>(false, ex.Message);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<AlertaCxcDTO>>(false, ex.Message);
        }
    }

    // 4. RESUMEN GENERAL
    public ApiResponse<ResumenCxcDTO> ObtenerResumenGeneral(UsuarioDTO ejecutor)
    {
        try
        {
            ejecutor.ValidarAcceso("abonos");
            var resumen = _factory.ObtenerTotalesCxc(ejecutor);
            return new ApiResponse<ResumenCxcDTO>(true, "Resumen generado", resumen);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<ResumenCxcDTO>(false, ex.Message);
        }
        catch (Exception ex)
        {
            return new ApiResponse<ResumenCxcDTO>(false, ex.Message);
        }
    }
}