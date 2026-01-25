using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Interfaces;

public interface IAbonoService
{
    ApiResponse<bool> ProcesarAbono(AbonoDTO abono, int clienteId, UsuarioDTO ejecutor);
    ApiResponse<List<CxcConsultaDTO>> BuscarCuentas(string filtro, UsuarioDTO ejecutor);
    ApiResponse<List<AlertaCxcDTO>> ObtenerAlertasMora(UsuarioDTO ejecutor);
    ApiResponse<ResumenCxcDTO> ObtenerResumenGeneral(UsuarioDTO ejecutor);
    ApiResponse<List<AbonoHistorialDTO>> ObtenerHistorial(int facturaId);
}