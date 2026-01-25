using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using System.Data;

namespace IntegraPro.AppLogic.Interfaces;

public interface IVentaService
{
    ApiResponse<string> ProcesarVenta(FacturaDTO factura, UsuarioDTO ejecutor);
    ApiResponse<DataTable> ObtenerReporteVentas(DateTime? desde, DateTime? hasta, int? clienteId, string busqueda, string? condicionVenta, UsuarioDTO ejecutor);
    ApiResponse<FacturaDTO> ObtenerFacturaParaImpresion(int id, UsuarioDTO ejecutor);

    // CORRECCIÓN: Ahora coincide con la implementación y la Factory
    ApiResponse<EmpresaDTO> ObtenerEmpresa(UsuarioDTO ejecutor);
}