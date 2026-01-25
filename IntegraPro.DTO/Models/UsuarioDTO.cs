using System;
using System.Collections.Generic;
using System.Text.Json;

namespace IntegraPro.DTO.Models;

public class UsuarioDTO
{
    public int Id { get; set; }
    public int RolId { get; set; }
    public int SucursalId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; } // Uso temporal en Login
    public string PasswordHash { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime? UltimoLogin { get; set; }
    public string? HardwareIdSesion { get; set; }
    public string? Token { get; set; }
    public string? NombreRol { get; set; }

    private string? _permisosJson;
    public string? PermisosJson
    {
        get => _permisosJson;
        set
        {
            _permisosJson = value;
            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                    // Limpieza de JSON escapado proveniente de JWT
                    string cleanJson = value.Trim('"').Replace("\\\"", "\"");
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    Permisos = JsonSerializer.Deserialize<Dictionary<string, bool>>(cleanJson, options) ?? new();
                }
                catch
                {
                    Permisos = new Dictionary<string, bool>();
                }
            }
            else
            {
                Permisos = new Dictionary<string, bool>();
            }
        }
    }

    public Dictionary<string, bool> Permisos { get; set; } = new();

    // ==========================================
    // MÉTODOS DE LÓGICA DE SEGURIDAD (NUEVOS)
    // ==========================================

    /// <summary>
    /// Evalúa si el usuario cuenta con un permiso específico.
    /// </summary>
    public bool TienePermiso(string clave)
    {
        if (Permisos == null) return false;

        // 1. RESTRICCIONES CRÍTICAS (Solo lectura)
        if (clave == "solo_lectura")
            return Permisos.TryGetValue("solo_lectura", out bool restringido) && restringido;

        // 2. ACCESO TOTAL (all)
        if (Permisos.TryGetValue("all", out bool all) && all) return true;

        // 3. PERMISO ESPECÍFICO
        return Permisos.TryGetValue(clave, out bool p) && p;
    }

    /// <summary>
    /// Lanza una excepción si el usuario tiene el perfil de "solo_lectura".
    /// Ideal para proteger métodos Insert, Update y Delete.
    /// </summary>
    public void ValidarEscritura()
    {
        if (TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Operación denegada: Su usuario solo tiene permisos de consulta (Auditoría).");
    }

    /// <summary>
    /// Valida si el usuario tiene acceso a un módulo. Si no, lanza excepción.
    /// </summary>
    public void ValidarAcceso(string clavePermiso)
    {
        if (!TienePermiso(clavePermiso))
            throw new UnauthorizedAccessException($"Acceso denegado: No cuenta con el permiso '{clavePermiso}' para realizar esta acción.");
    }

    /// <summary>
    /// Genera dinámicamente el fragmento SQL para filtrar por sucursal.
    /// </summary>
    /// <param name="aliasTabla">Opcional: alias de la tabla en el SQL (ej: 'p')</param>
    public string GetFiltroSucursal(string aliasTabla = "")
    {
        // Si no tiene la limitación, devolvemos una condición siempre verdadera.
        if (!TienePermiso("sucursal_limit")) return " (1=1) ";

        string prefijo = string.IsNullOrEmpty(aliasTabla) ? "" : $"{aliasTabla}.";
        return $" {prefijo}sucursal_id = {this.SucursalId} ";
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}