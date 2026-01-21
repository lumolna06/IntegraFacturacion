namespace IntegraPro.DTO.Models;

public class PagoCxpDTO
{
    public int CompraId { get; set; }
    public int UsuarioId { get; set; }
    public decimal Monto { get; set; }
    public string? NumeroReferencia { get; set; }
    public string? Notas { get; set; }
}