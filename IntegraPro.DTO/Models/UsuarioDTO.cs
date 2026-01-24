namespace IntegraPro.DTO.Models;

public class UsuarioDTO
{
    public int Id { get; set; }
    public int RolId { get; set; }
    public int SucursalId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime? UltimoLogin { get; set; }
    public string? HardwareIdSesion { get; set; }

    // --- PROPIEDADES DE APOYO (Solo Lectura para el Front) ---
    // Estas se llenan mediante un JOIN en el UsuarioFactory
    public string? NombreRol { get; set; }      // Ej: "Administrador"
    public string? PermisosJson { get; set; }  // Ej: {"all": true}
}

// Clase de apoyo para recibir credenciales
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}