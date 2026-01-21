namespace IntegraPro.DTO.Models;

public class PagoHistorialDTO
{
    public int Id { get; set; }
    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; }
    public string? NumeroReferencia { get; set; }
    public string? Notas { get; set; }
    public string? NombreUsuario { get; set; } // Para saber quién aplicó el pago
}