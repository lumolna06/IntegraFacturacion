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
    public string Estado { get; set; } = "Procesada"; // Procesada o Anulada
    public string TipoPago { get; set; } = "Contado"; // Contado o Credito
    public int DiasCredito { get; set; } // Plazo para pagar si es a crédito

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
}