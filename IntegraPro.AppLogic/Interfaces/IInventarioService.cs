using IntegraPro.DTO.Models;
using IntegraPro.AppLogic.Utils;

namespace IntegraPro.AppLogic.Interfaces;

public interface IInventarioService
{
    /// <summary>
    /// Registra un movimiento manual (Entrada/Salida/Ajuste).
    /// El ejecutor define la sucursal y la auditoría del movimiento.
    /// </summary>
    ApiResponse<bool> Registrar(MovimientoInventarioDTO mov, UsuarioDTO ejecutor);

    /// <summary>
    /// Realiza la explosión de materiales (BOM) para generar stock de un producto elaborado.
    /// Valida existencias de insumos en la sucursal del ejecutor.
    /// </summary>
    ApiResponse<bool> ProcesarProduccion(int productoPadreId, decimal cantidadAProducir, UsuarioDTO ejecutor);
}