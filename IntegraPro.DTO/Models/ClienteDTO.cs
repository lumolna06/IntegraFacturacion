namespace IntegraPro.DTO.Models;

public class ClienteDTO
{
    public int Id { get; set; }
    public string Identificacion { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public decimal LimiteCredito { get; set; }
    public bool Activo { get; set; } = true;
}