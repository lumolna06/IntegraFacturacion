namespace IntegraPro.DTO.Models;

public class CxcConsultaDTO
{
    public int FacturaId { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteCedula { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string Estado { get; set; } = string.Empty; // Pendiente, Pagada, Vencida
    public int DiasCredito { get; set; }
}