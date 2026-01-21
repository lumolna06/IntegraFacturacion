namespace IntegraPro.DTO.Models;

public class AlertaPagoDTO
{
    public int Id { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public decimal SaldoPendiente { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public int DiasParaVencimiento { get; set; }
    public string Urgencia { get; set; } = string.Empty;
}