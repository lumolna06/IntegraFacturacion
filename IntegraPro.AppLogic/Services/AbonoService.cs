using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class AbonoService(AbonoFactory factory) : IAbonoService
{
    private readonly AbonoFactory _factory = factory;

    // 1. PROCESAR ABONO (Sincronizado con ProcesarAbonoCompleto)
    public ApiResponse<bool> ProcesarAbono(AbonoDTO abono, int clienteId, UsuarioDTO ejecutor)
    {
        try
        {
            // Validaciones básicas de negocio
            if (abono.MontoAbonado <= 0)
                return new ApiResponse<bool>(false, "El monto del abono debe ser mayor a cero.", false);

            // Llamada al Factory con los 3 parámetros requeridos
            _factory.ProcesarAbonoCompleto(abono, clienteId, ejecutor);

            return new ApiResponse<bool>(true, "Abono procesado y saldos actualizados", true);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Captura el bloqueo de 'solo_lectura' definido en el Factory
            return new ApiResponse<bool>(false, ex.Message, false);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, $"Error en la operación: {ex.Message}", false);
        }
    }

    // 2. BUSCAR CUENTAS (Sincronizado con BuscarCxcClientes)
    public ApiResponse<List<CxcConsultaDTO>> BuscarCuentas(string filtro, UsuarioDTO ejecutor)
    {
        try
        {
            var datos = _factory.BuscarCxcClientes(filtro, ejecutor);
            return new ApiResponse<List<CxcConsultaDTO>>(true, "Cuentas localizadas", datos);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<CxcConsultaDTO>>(false, ex.Message);
        }
    }

    // Agrega este método dentro de la clase AbonoService
    public ApiResponse<List<AbonoHistorialDTO>> ObtenerHistorial(int facturaId)
    {
        try
        {
            var lista = _factory.ListarHistorialAbonos(facturaId);
            return new ApiResponse<List<AbonoHistorialDTO>>(true, "Historial cargado", lista);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<AbonoHistorialDTO>>(false, $"Error al cargar historial: {ex.Message}");
        }
    }


    // 3. ALERTAS DE MORA (Sincronizado con ObtenerAlertasVencimiento)
    public ApiResponse<List<AlertaCxcDTO>> ObtenerAlertasMora(UsuarioDTO ejecutor)
    {
        try
        {
            var alertas = _factory.ObtenerAlertasVencimiento(ejecutor);
            return new ApiResponse<List<AlertaCxcDTO>>(true, "Alertas de mora obtenidas", alertas);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<AlertaCxcDTO>>(false, ex.Message);
        }
    }

    // 4. RESUMEN GENERAL (Sincronizado con ObtenerTotalesCxc)
    public ApiResponse<ResumenCxcDTO> ObtenerResumenGeneral(UsuarioDTO ejecutor)
    {
        try
        {
            var resumen = _factory.ObtenerTotalesCxc(ejecutor);
            return new ApiResponse<ResumenCxcDTO>(true, "Resumen generado", resumen);
        }
        catch (Exception ex)
        {
            return new ApiResponse<ResumenCxcDTO>(false, ex.Message);
        }
    }
}