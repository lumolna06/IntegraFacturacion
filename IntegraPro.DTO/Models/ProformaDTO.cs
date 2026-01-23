namespace IntegraPro.DTO.Models;

public class ProformaEncabezadoDTO
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? ClienteIdentificacion { get; set; }
    public int SucursalId { get; set; }
    public DateTime Fecha { get; set; }
    public DateTime FechaVencimiento { get; set; }

    // --- Totales del Encabezado (Suma de todos los detalles) ---
    public decimal TotalNeto { get; set; }      // Subtotal sin impuestos
    public decimal TotalImpuesto { get; set; }  // Total de IVAs acumulados
    public decimal Total { get; set; }          // Monto final (Neto + Impuesto)

    public string Estado { get; set; } = "Pendiente";
    public List<ProformaDetalleDTO> Detalles { get; set; } = new();
}

public class ProformaDetalleDTO
{
    public int ProductoId { get; set; }
    public string? ProductoCodigo { get; set; }
    public string? ProductoNombre { get; set; }
    public decimal Cantidad { get; set; }

    // --- Desglose por Línea ---
    public decimal PrecioUnitario { get; set; }    // Precio Neto (sacado de 'costo_actual')
    public decimal PorcentajeImpuesto { get; set; } // El % aplicado (ej: 13, 1, 0)
    public decimal ImpuestoTotal { get; set; }      // El monto de dinero del impuesto para esta línea
    public decimal TotalLineas { get; set; }        // (Cantidad * PrecioUnitario) + ImpuestoTotal
}