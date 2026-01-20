using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Interfaces;

public interface IInventarioService
{
    ApiResponse<bool> Registrar(MovimientoInventarioDTO mov);

    // AÑADE ESTA LÍNEA:
    ApiResponse<bool> ProcesarProduccion(int productoPadreId, decimal cantidadAProducir, int usuarioId);
}