namespace IntegraPro.DTO.Models;

public class FacturaDTO
{
    // Encabezado
    public int ClienteId { get; set; }
    public int SucursalId { get; set; }
    public int UsuarioId { get; set; }
    public string CondicionVenta { get; set; } = "Contado"; // Contado o Credito
    public string MedioPago { get; set; } = "Efectivo";
    public string? Notas { get; set; }

    // Detalle de productos
    public List<FacturaDetalleDTO> Detalles { get; set; } = new();

    // Totales calculados (pueden venir del Front o calcularse en el Service)
    public decimal TotalNeto { get; set; }
    public decimal TotalImpuesto { get; set; }
    public decimal TotalComprobante { get; set; }
}

public class FacturaDetalleDTO
{
    public int ProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PorcentajeDescuento { get; set; }
}