using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Interfaces;

public interface ICajaService
{
    ApiResponse<int> AbrirCaja(CajaAperturaDTO apertura, UsuarioDTO ejecutor);
    ApiResponse<bool> CerrarCaja(CajaCierreDTO cierre, UsuarioDTO ejecutor);
    ApiResponse<List<object>> ObtenerHistorial(UsuarioDTO ejecutor);
}