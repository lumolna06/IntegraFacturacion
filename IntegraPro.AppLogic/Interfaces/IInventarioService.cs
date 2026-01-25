using IntegraPro.DTO.Models;
using IntegraPro.AppLogic.Utils;

namespace IntegraPro.AppLogic.Interfaces;

public interface IInventarioService
{
    // Cambiamos las firmas para incluir al UsuarioDTO ejecutor
    ApiResponse<bool> Registrar(MovimientoInventarioDTO mov, UsuarioDTO ejecutor);

    ApiResponse<bool> ProcesarProduccion(int productoPadreId, decimal cantidadAProducir, UsuarioDTO ejecutor);
}