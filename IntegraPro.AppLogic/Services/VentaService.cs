using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class VentaService(VentaFactory ventaFactory, ClienteFactory cliFactory, ProductoFactory prodFactory)
{
    private readonly VentaFactory _ventaFactory = ventaFactory;
    private readonly ClienteFactory _cliFactory = cliFactory;
    private readonly ProductoFactory _prodFactory = prodFactory;

    public string ProcesarVenta(FacturaDTO factura)
    {
        // 1. VALIDACIÓN DE STOCK (Evitar negativos)
        foreach (var det in factura.Detalles)
        {
            // USAMOS 'GetById' que es el nombre real en tu ProductoFactory
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

            if (!activo)
                throw new Exception("El cliente seleccionado se encuentra INACTIVO.");

            if ((saldoActual + factura.TotalComprobante) > limite)
            {
                throw new Exception($"Límite de crédito excedido. " +
                                    $"Saldo actual: {saldoActual:N2}, " +
                                    $"Límite: {limite:N2}. " +
                                    $"Total factura: {factura.TotalComprobante:N2}.");
            }
        }

        // 3. EJECUCIÓN TRANSACCIONAL ÚNICA
        // Al llegar aquí, ya validamos que hay stock y crédito.
        string consecutivo = _ventaFactory.CrearFactura(factura);

        // 4. ACTUALIZAR SALDO DEL CLIENTE
        if (factura.CondicionVenta.Equals("Credito", StringComparison.OrdinalIgnoreCase))
        {
            _cliFactory.ActualizarSaldo(factura.ClienteId, factura.TotalComprobante);
        }

        return consecutivo;
    }
}