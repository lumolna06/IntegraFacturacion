using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class VentaService(string connectionString)
{
    private readonly FacturaFactory _facFactory = new(connectionString);
    private readonly InventarioFactory _invFactory = new(connectionString);
    private readonly ClienteFactory _cliFactory = new(connectionString); // Añadido para validar crédito

    public string ProcesarVenta(FacturaDTO factura)
    {
        // 1. VALIDACIÓN DE CRÉDITO (Si aplica)
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

        // 2. Generar Consecutivo (Simulado)
        string consecutivo = "FAC-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        string clave = "CR" + DateTime.Now.Ticks.ToString().Substring(0, 10);

        // 3. Crear Encabezado de Factura
        int facturaId = _facFactory.CrearEncabezado(factura, consecutivo, clave);

        // 4. Procesar Líneas del Detalle e Inventario
        foreach (var item in factura.Detalles)
        {
            // A. Guardar en FACTURA_DETALLE
            _facFactory.InsertarDetalle(facturaId, item);

            // B. Movimiento de Inventario (SALIDA)
            var mov = new MovimientoInventarioDTO
            {
                ProductoId = item.ProductoId,
                UsuarioId = factura.UsuarioId,
                TipoMovimiento = "SALIDA",
                Cantidad = item.Cantidad,
                DocumentoReferencia = "Venta: " + consecutivo,
                Notas = "Salida automática por facturación"
            };

            _invFactory.Insertar(mov);
        }

        // 5. ACTUALIZAR SALDO DEL CLIENTE (Solo si la venta fue exitosa y es a crédito)
        if (factura.CondicionVenta.Equals("Credito", StringComparison.OrdinalIgnoreCase))
        {
            _cliFactory.ActualizarSaldo(factura.ClienteId, factura.TotalComprobante);
        }

        return consecutivo;
    }
}