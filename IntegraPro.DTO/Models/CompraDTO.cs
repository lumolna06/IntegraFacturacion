namespace IntegraPro.DTO.Models;

public class CompraDTO
{
    // --- Encabezado ---
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public int SucursalId { get; set; }
    public int UsuarioId { get; set; }
    public string? NumeroFacturaProveedor { get; set; }
    public DateTime FechaCompra { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalImpuestos { get; set; }
    public decimal TotalCompra { get; set; }
    public string? Notas { get; set; }

    // --- Lógica de Negocio y Estado ---
    public string Estado { get; set; } = "Procesada";
    public string TipoPago { get; set; } = "Contado";
    public int DiasCredito { get; set; }

    // --- Detalle Unificado ---
    public List<CompraDetalleDTO> Detalles { get; set; } = new();
}

public class CompraDetalleDTO
{
    public int ProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoUnitarioNeto { get; set; }
    public decimal MontoImpuesto { get; set; }
    public decimal TotalLinea { get; set; }

    // --- IMPORTANTE: Propiedades para el flujo XML ---
    /// <summary>
    /// Código CABYS o Comercial que viene en el XML. 
    /// Se usa para crear la equivalencia automática.
    /// </summary>
    public string? CodigoCabys { get; set; }

    /// <summary>
    /// Nombre tal cual viene en el XML (ej: "ENERGIA"). 
    /// Ayuda al usuario a identificar el producto en la pantalla de mapeo.
    /// </summary>
    public string? DetalleOriginalXml { get; set; }
}