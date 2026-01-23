using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;
using System.Data; // Necesario para manejar el reporte como DataTable

namespace IntegraPro.AppLogic.Services;

public class VentaService(
    VentaFactory ventaFactory,
    ClienteFactory cliFactory,
    ProductoFactory prodFactory,
    ConfiguracionFactory configFactory)
{
    private readonly VentaFactory _ventaFactory = ventaFactory;
    private readonly ClienteFactory _cliFactory = cliFactory;
    private readonly ProductoFactory _prodFactory = prodFactory;
    private readonly ConfiguracionFactory _configFactory = configFactory;

    public string ProcesarVenta(FacturaDTO factura)
    {
        // 1. VALIDACIÓN DE STOCK
        foreach (var det in factura.Detalles)
        {
            var producto = _prodFactory.GetById(det.ProductoId);
            if (producto == null)
                throw new Exception($"El producto con ID {det.ProductoId} no existe.");

            if (producto.Existencia < det.Cantidad)
            {
                throw new Exception($"Stock insuficiente para '{producto.Nombre}'. " +
                                    $"Solicitado: {det.Cantidad:N2}, Disponible: {producto.Existencia:N2}.");
            }
        }

        // 2. VALIDACIÓN DE CRÉDITO
        if (factura.CondicionVenta.Equals("Credito", StringComparison.OrdinalIgnoreCase))
        {
            var (saldoActual, limite, activo) = _cliFactory.ObtenerEstadoCredito(factura.ClienteId);
            if (!activo) throw new Exception("El cliente seleccionado se encuentra INACTIVO.");

            if ((saldoActual + factura.TotalComprobante) > limite)
            {
                throw new Exception($"Límite de crédito excedido. Saldo: {saldoActual:N2}, Límite: {limite:N2}.");
            }
        }

        // 3. EJECUCIÓN
        string consecutivoGenerado = _ventaFactory.CrearFactura(factura);

        // 4. ACTUALIZAR SALDO (Solo si es Crédito)
        if (factura.CondicionVenta.Equals("Credito", StringComparison.OrdinalIgnoreCase))
        {
            _cliFactory.ActualizarSaldo(factura.ClienteId, factura.TotalComprobante);
        }

        return consecutivoGenerado;
    }

    // ==========================================
    // MÉTODO PARA REPORTES (ACTUALIZADO CON CONDICIÓN)
    // ==========================================
    public DataTable ObtenerReporteVentas(DateTime? desde, DateTime? hasta, int? clienteId, int? sucursalId, string busqueda, string? condicionVenta)
    {
        // Ahora pasamos también el parámetro de condición (Contado/Crédito)
        return _ventaFactory.ObtenerReporteVentas(desde, hasta, clienteId, sucursalId, busqueda, condicionVenta);
    }

    public FacturaDTO ObtenerFacturaParaImpresion(int id)
    {
        var factura = _ventaFactory.ObtenerPorId(id);
        if (factura != null)
        {
            factura.Detalles = _ventaFactory.ListarDetalles(id);
        }
        return factura;
    }

    public EmpresaDTO ObtenerEmpresa()
    {
        return _configFactory.ObtenerEmpresa();
    }
}