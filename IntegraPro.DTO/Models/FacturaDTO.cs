namespace IntegraPro.DTO.Models;

public class FacturaDTO
{
    // --- Encabezado ---
    public int ClienteId { get; set; }
    public int SucursalId { get; set; }
    public int UsuarioId { get; set; }

    // --- PROPIEDADES PARA FACTURACIÓN ELECTRÓNICA / LOCAL ---
    // El Factory usará estos si vienen del Front, o los generará si son null
    public string? Consecutivo { get; set; }
    public string? ClaveNumerica { get; set; }

    public string CondicionVenta { get; set; } = "Contado"; // Contado o Credito
    public string MedioPago { get; set; } = "Efectivo";
    public string? Notas { get; set; }

    // Detalle de productos
    public List<FacturaDetalleDTO> Detalles { get; set; } = new();

    // Totales calculados (Suma de todas las líneas)
    public decimal TotalNeto { get; set; }
    public decimal TotalImpuesto { get; set; }
    public decimal TotalComprobante { get; set; }
}

public class FacturaDetalleDTO
{
    public int ProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    // --- PROPIEDAD CRÍTICA PARA LA SOLUCIÓN ROBUSTA ---
    // Permite manejar 13%, 1%, 2%, 4% o 0% por cada producto individualmente
    public decimal PorcentajeImpuesto { get; set; }

    public decimal PorcentajeDescuento { get; set; }
}