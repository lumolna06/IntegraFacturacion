namespace IntegraPro.DTO.Models;

public class CajaAperturaDTO
{
    public int SucursalId { get; set; }
    public int UsuarioId { get; set; }
    public decimal MontoInicial { get; set; }
}

public class CajaCierreDTO
{
    public int CajaId { get; set; }
    public decimal MontoRealEnCaja { get; set; }
    public string? Notas { get; set; }
}