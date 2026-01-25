using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using System.Collections.Generic; // Asegúrate de tener este using para List<>

namespace IntegraPro.AppLogic.Interfaces;

public interface IAbonoService
{
    ApiResponse<bool> ProcesarAbono(AbonoDTO abono, int clienteId, UsuarioDTO ejecutor);
    
    ApiResponse<List<CxcConsultaDTO>> BuscarCuentas(string filtro, UsuarioDTO ejecutor);
    
    ApiResponse<List<AlertaCxcDTO>> ObtenerAlertasMora(UsuarioDTO ejecutor);
    
    ApiResponse<ResumenCxcDTO> ObtenerResumenGeneral(UsuarioDTO ejecutor);
    
    // Añadimos ejecutor para que el Service pueda validar permisos antes de listar
    ApiResponse<List<AbonoHistorialDTO>> ObtenerHistorial(int facturaId, UsuarioDTO ejecutor);
}