namespace IntegraPro.DTO.Models;

public class AbonoDTO
{
    public int CuentaCobrarId { get; set; }
    public int UsuarioId { get; set; }
    public int SucursalId { get; set; }
    public decimal MontoAbonado { get; set; }
    public string MedioPago { get; set; } = "Efectivo";
    public string? Referencia { get; set; }
}