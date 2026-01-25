using Microsoft.AspNetCore.Mvc;
using IntegraPro.DTO.Models;
using System.Security.Claims;
using System.Diagnostics;
using System.Linq;

namespace IntegraPro.API.Controllers;

/// <summary>
/// Controlador base para centralizar la obtención del usuario autenticado y sus permisos.
/// </summary>
public class BaseController : ControllerBase
{
    protected UsuarioDTO UsuarioActual
    {
        get
        {
            // Verificamos si hay un token válido y el usuario está autenticado
            if (User.Identity?.IsAuthenticated == true)
            {
                // Obtenemos la lista de claims una sola vez para mejorar el rendimiento
                var claims = User.Claims.ToList();

                // Buscamos los valores de forma insensible a mayúsculas (Case-Insensitive)
                // Esto evita que el sistema falle si el token trae "id" pero buscamos "Id"
                var userIdStr = claims.FirstOrDefault(c => c.Type.Equals("id", StringComparison.OrdinalIgnoreCase))?.Value
                                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? "0";

                var rolIdStr = claims.FirstOrDefault(c => c.Type.Equals("rolId", StringComparison.OrdinalIgnoreCase))?.Value
                               ?? "0";

                var sucursalIdStr = claims.FirstOrDefault(c => c.Type.Equals("sucursalId", StringComparison.OrdinalIgnoreCase))?.Value
                                    ?? "0";

                var permisosRaw = claims.FirstOrDefault(c => c.Type.Equals("permisos", StringComparison.OrdinalIgnoreCase))?.Value;

                // --- BLOQUE DE DEPURACIÓN EN CONSOLA ---
                Debug.WriteLine("================ AUTH DEBUG START ================");
                Debug.WriteLine($"[USUARIO]: {User.Identity.Name}");
                Debug.WriteLine($"[ID]: {userIdStr}");
                Debug.WriteLine($"[ROL]: {rolIdStr}");
                Debug.WriteLine($"[SUCURSAL]: {sucursalIdStr}");
                Debug.WriteLine($"[PERMISOS RAW]: {permisosRaw}");

                // Tip: Imprimimos todos los claims si permisosRaw llega nulo para ver qué nombres traen
                if (string.IsNullOrEmpty(permisosRaw))
                {
                    Debug.WriteLine("[WARN] No se encontró el claim 'permisos'. Claims disponibles:");
                    foreach (var c in claims) Debug.WriteLine($" -> {c.Type}: {c.Value}");
                }
                Debug.WriteLine("================= AUTH DEBUG END =================");

                return new UsuarioDTO
                {
                    Id = int.Parse(userIdStr),
                    RolId = int.Parse(rolIdStr),
                    SucursalId = int.Parse(sucursalIdStr),
                    Username = User.Identity.Name ?? "Sin nombre",
                    // Al asignar PermisosJson, el setter en UsuarioDTO hará el resto
                    PermisosJson = permisosRaw ?? "{}"
                };
            }

            // Fallback para usuarios no autenticados
            return new UsuarioDTO
            {
                Id = 0,
                Username = "Anonimo",
                RolId = 0,
                SucursalId = 0,
                PermisosJson = "{}"
            };
        }
    }
}