namespace IntegraPro.DTO.Models;

public class AbonoHistorialDTO
{
    public int Id { get; set; }
    public decimal Monto { get; set; }
    public DateTime FechaAbono { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public string NumeroReferencia { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty; // Quién recibió el dinero
    public string Notas { get; set; } = string.Empty;
}