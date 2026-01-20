using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Interfaces;

public interface IInventarioService
{
    ApiResponse<bool> Registrar(MovimientoInventarioDTO mov);
}