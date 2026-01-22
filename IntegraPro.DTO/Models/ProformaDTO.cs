namespace IntegraPro.DTO.Models;

public class ProformaEncabezadoDTO
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public int SucursalId { get; set; }
    public DateTime Fecha { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public List<ProformaDetalleDTO> Detalles { get; set; } = new();
}

public class ProformaDetalleDTO
{
    public int ProductoId { get; set; }
    public string? ProductoNombre { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal TotalLineas { get; set; }
}