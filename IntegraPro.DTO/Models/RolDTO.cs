namespace IntegraPro.DTO.Models;

public class RolDTO
{
    public int Id { get; set; }
    public string NombreRol { get; set; } = string.Empty;
    public string PermisosJson { get; set; } = "{}";
}
