namespace IntegraPro.DTO.Models;

public class RegistroInicialDTO
{
    // Datos para el Usuario Administrador
    public string NombreCompleto { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;

    // Datos para la Empresa
    public string NombreComercial { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string CedulaJuridica { get; set; } = string.Empty;
    public string TipoRegimen { get; set; } = "Simplificado";
    public string Telefono { get; set; } = string.Empty;
    public bool PermitirStockNegativo { get; set; }
}