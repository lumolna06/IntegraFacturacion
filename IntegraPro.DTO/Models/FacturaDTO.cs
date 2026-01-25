namespace IntegraPro.DTO.Models;

public class FacturaDTO
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteIdentificacion { get; set; }
    public int SucursalId { get; set; }
    public int UsuarioId { get; set; }
    public string? Consecutivo { get; set; }
    public string? ClaveNumerica { get; set; }
    public DateTime Fecha { get; set; }
    public string CondicionVenta { get; set; } = "Contado";
    public string MedioPago { get; set; } = "Efectivo";

    public decimal TotalNeto { get; set; }
    public decimal TotalDescuento { get; set; } // Ahora es decimal estándar (no nulo)
    public decimal TotalImpuesto { get; set; }
    public decimal TotalComprobante { get; set; }

    public string? EstadoHacienda { get; set; } = "LOCAL";
    public bool EsOffline { get; set; } = true;

    public List<FacturaDetalleDTO> Detalles { get; set; } = new();
}

public class FacturaDetalleDTO
{
    public int ProductoId { get; set; }
    public string? ProductoNombre { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PorcentajeImpuesto { get; set; } = 13;
    public decimal MontoImpuesto { get; set; } // Ahora permite asignación
    public decimal PorcentajeDescuento { get; set; }
    public decimal MontoDescuento { get; set; } // Ahora permite asignación
    public decimal TotalLinea { get; set; } // Ahora permite asignación
}