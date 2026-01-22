namespace IntegraPro.DTO.Models;

public class AlertaCxcDTO
{
    public int FacturaId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string NumeroFactura { get; set; } = string.Empty;
    public decimal SaldoPendiente { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public int DiasVencidos { get; set; } // Negativo si falta para vencer, positivo si ya venció
    public string NivelRiesgo { get; set; } = string.Empty; // Bajo, Medio, Alto (Mora)
}