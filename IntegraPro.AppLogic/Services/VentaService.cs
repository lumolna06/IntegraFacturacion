using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;
using System.Data;

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

    public string ProcesarVenta(FacturaDTO factura, UsuarioDTO ejecutor)
    {
        // 1. VALIDACIÓN DE STOCK (Considerando configuración de empresa)
        var empresa = _configFactory.ObtenerEmpresa();
        bool permitirNegativo = empresa?.PermitirStockNegativo ?? false;

        if (!permitirNegativo)
        {
            foreach (var det in factura.Detalles)
            {
                // Usamos el ejecutor para asegurar que el producto se busque en su sucursal
                var producto = _prodFactory.GetById(det.ProductoId, ejecutor);

                if (producto == null)
                    throw new Exception($"El producto con ID {det.ProductoId} no está disponible en su sucursal.");

                if (producto.Existencia < det.Cantidad)
                {
                    throw new Exception($"Stock insuficiente para '{producto.Nombre}'. " +
                                        $"Solicitado: {det.Cantidad:N2}, Disponible: {producto.Existencia:N2}.");
                }
            }
        }

        // 2. VALIDACIÓN DE CRÉDITO
        if (factura.CondicionVenta != null && factura.CondicionVenta.Equals("Credito", StringComparison.OrdinalIgnoreCase))
        {
            var (saldoActual, limite, activo) = _cliFactory.ObtenerEstadoCredito(factura.ClienteId);
            if (!activo) throw new Exception("El cliente seleccionado se encuentra INACTIVO.");

            if ((saldoActual + factura.TotalComprobante) > limite)
            {
                throw new Exception($"Límite de crédito excedido. Saldo: {saldoActual:N2}, Límite: {limite:N2}.");
            }
        }

        // 3. EJECUCIÓN (Pasando el ejecutor para auditoría y sucursal_limit)
        string consecutivoGenerado = _ventaFactory.CrearFactura(factura, ejecutor);

        // 4. ACTUALIZAR SALDO (Solo si es Crédito)
        if (factura.CondicionVenta != null && factura.CondicionVenta.Equals("Credito", StringComparison.OrdinalIgnoreCase))
        {
            _cliFactory.ActualizarSaldo(factura.ClienteId, factura.TotalComprobante);
        }

        return consecutivoGenerado;
    }

    /// <summary>
    /// Obtiene el reporte de ventas aplicando filtros y seguridad de sucursal.
    /// </summary>
    public DataTable ObtenerReporteVentas(DateTime? desde, DateTime? hasta, int? clienteId, string busqueda, string? condicionVenta, UsuarioDTO ejecutor)
    {
        return _ventaFactory.ObtenerReporteVentas(desde, hasta, clienteId, busqueda, condicionVenta ?? "", ejecutor);
    }

    public FacturaDTO? ObtenerFacturaParaImpresion(int id, UsuarioDTO ejecutor)
    {
        var factura = _ventaFactory.ObtenerPorId(id, ejecutor);

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