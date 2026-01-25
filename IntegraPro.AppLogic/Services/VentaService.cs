using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;
using System.Data;

namespace IntegraPro.AppLogic.Services;

public class VentaService(
    VentaFactory ventaFactory,
    ClienteFactory cliFactory,
    ProductoFactory prodFactory,
    ConfiguracionFactory configFactory) : IVentaService
{
    private readonly VentaFactory _ventaFactory = ventaFactory;
    private readonly ClienteFactory _cliFactory = cliFactory;
    private readonly ProductoFactory _prodFactory = prodFactory;
    private readonly ConfiguracionFactory _configFactory = configFactory;

    public ApiResponse<string> ProcesarVenta(FacturaDTO factura, UsuarioDTO ejecutor)
    {
        try
        {
            // CORRECCIÓN: Ahora pasamos el ejecutor a ObtenerEmpresa
            var empresa = _configFactory.ObtenerEmpresa(ejecutor);
            bool permitirNegativo = empresa?.PermitirStockNegativo ?? false;

            if (!permitirNegativo)
            {
                foreach (var det in factura.Detalles)
                {
                    var producto = _prodFactory.GetById(det.ProductoId, ejecutor);

                    if (producto == null)
                        return new ApiResponse<string>(false, $"El producto ID {det.ProductoId} no está disponible.");

                    if (producto.Existencia < det.Cantidad)
                    {
                        return new ApiResponse<string>(false, $"Stock insuficiente para '{producto.Nombre}'. " +
                                                              $"Solicitado: {det.Cantidad:N2}, Disponible: {producto.Existencia:N2}.");
                    }
                }
            }

            // 2. VALIDACIÓN DE CRÉDITO
            if (factura.CondicionVenta != null && factura.CondicionVenta.Equals("Credito", StringComparison.OrdinalIgnoreCase))
            {
                var (saldoActual, limite, activo) = _cliFactory.ObtenerEstadoCredito(factura.ClienteId);
                if (!activo) return new ApiResponse<string>(false, "El cliente seleccionado se encuentra INACTIVO para crédito.");

                if ((saldoActual + factura.TotalComprobante) > limite)
                {
                    return new ApiResponse<string>(false, $"Límite de crédito excedido. Saldo: {saldoActual:N2}, Límite: {limite:N2}.");
                }
            }

            // 3. EJECUCIÓN (Ya pasaba el ejecutor correctamente)
            string consecutivoGenerado = _ventaFactory.CrearFactura(factura, ejecutor);

            // 4. ACTUALIZAR SALDO
            if (factura.CondicionVenta != null && factura.CondicionVenta.Equals("Credito", StringComparison.OrdinalIgnoreCase))
            {
                _cliFactory.ActualizarSaldo(factura.ClienteId, factura.TotalComprobante);
            }

            return new ApiResponse<string>(true, "Venta procesada con éxito", consecutivoGenerado);
        }
        catch (Exception ex)
        {
            Logger.WriteLog("Error", "ProcesarVenta", ex.Message);
            return new ApiResponse<string>(false, $"Error al procesar la venta: {ex.Message}");
        }
    }

    public ApiResponse<DataTable> ObtenerReporteVentas(DateTime? desde, DateTime? hasta, int? clienteId, string busqueda, string? condicionVenta, UsuarioDTO ejecutor)
    {
        try
        {
            var dt = _ventaFactory.ObtenerReporteVentas(desde, hasta, clienteId, busqueda, condicionVenta ?? "", ejecutor);
            return new ApiResponse<DataTable>(true, "Reporte generado", dt);
        }
        catch (Exception ex)
        {
            return new ApiResponse<DataTable>(false, ex.Message);
        }
    }

    public ApiResponse<FacturaDTO> ObtenerFacturaParaImpresion(int id, UsuarioDTO ejecutor)
    {
        try
        {
            var factura = _ventaFactory.ObtenerPorId(id, ejecutor);
            if (factura != null)
            {
                factura.Detalles = _ventaFactory.ListarDetalles(id);
                return new ApiResponse<FacturaDTO>(true, "Factura cargada", factura);
            }
            return new ApiResponse<FacturaDTO>(false, "No se encontró la factura.");
        }
        catch (Exception ex)
        {
            return new ApiResponse<FacturaDTO>(false, ex.Message);
        }
    }

    public ApiResponse<EmpresaDTO> ObtenerEmpresa(UsuarioDTO ejecutor)
    {
        try
        {
            // CORRECCIÓN: Se agrega el parámetro ejecutor al método y se pasa a la Factory
            return new ApiResponse<EmpresaDTO>(true, "Datos de empresa cargados", _configFactory.ObtenerEmpresa(ejecutor));
        }
        catch (Exception ex)
        {
            return new ApiResponse<EmpresaDTO>(false, ex.Message);
        }
    }
}