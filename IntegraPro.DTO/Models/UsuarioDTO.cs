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
                    // --- CORRECCIÓN PARA JWT ---
                    // Al recibir el JSON del Token, suele venir escapado como "{\"all\":true}"
                    // Trim('"') quita comillas externas y Replace limpia las barras invertidas internas
                    string cleanJson = value.Trim('"').Replace("\\\"", "\"");

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    Permisos = JsonSerializer.Deserialize<Dictionary<string, bool>>(cleanJson, options) ?? new();
                }
                catch
                {
                    // Si el JSON es inválido, inicializamos el diccionario vacío para evitar errores
                    Permisos = new Dictionary<string, bool>();
                }
            }
            else
            {
                Permisos = new Dictionary<string, bool>();
            }
        }
    }

    // Diccionario para lógica interna de Factories (se llena automáticamente vía PermisosJson)
    public Dictionary<string, bool> Permisos { get; set; } = new();

    /// <summary>
    /// Evalúa si el usuario cuenta con un permiso específico.
    /// </summary>
    public bool TienePermiso(string clave)
    {
        if (Permisos == null) return false;

        // 1. RESTRICCIONES CRÍTICAS (Solo lectura tiene prioridad de bloqueo)
        if (clave == "solo_lectura")
        {
            return Permisos.TryGetValue("solo_lectura", out bool restringido) && restringido;
        }

        // 2. ACCESO TOTAL (Permiso "all" otorga todo excepto si hay restricción de solo lectura)
        if (Permisos.TryGetValue("all", out bool all) && all) return true;

        // 3. PERMISO ESPECÍFICO
        return Permisos.TryGetValue(clave, out bool p) && p;
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}