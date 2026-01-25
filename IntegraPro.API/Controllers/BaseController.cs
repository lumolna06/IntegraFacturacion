using Microsoft.AspNetCore.Mvc;
using IntegraPro.DTO.Models;
using System.Security.Claims;
using System.Diagnostics; // Necesario para Debug.WriteLine

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
                // Extraemos los valores de los claims
                // Usamos tanto el tipo estándar como el nombre literal por seguridad
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("nameid")?.Value ?? "0";
                var rolIdClaim = User.FindFirst("RolId")?.Value ?? "0";
                var sucursalIdClaim = User.FindFirst("SucursalId")?.Value ?? "0";
                var permisosRaw = User.FindFirst("Permisos")?.Value;

                // --- BLOQUE DE DEPURACIÓN EN CONSOLA ---
                // Revisa la ventana "Salida" (Output) de Visual Studio mientras ejecutas
                Debug.WriteLine("========================================");
                Debug.WriteLine($"[AUTH DEBUG] Usuario: {User.Identity.Name}");
                Debug.WriteLine($"[AUTH DEBUG] ID Extraído: {userIdClaim}");
                Debug.WriteLine($"[AUTH DEBUG] Permisos RAW: {permisosRaw}");
                Debug.WriteLine("========================================");

                var usuario = new UsuarioDTO
                {
                    Id = int.Parse(userIdClaim),
                    RolId = int.Parse(rolIdClaim),
                    SucursalId = int.Parse(sucursalIdClaim),
                    Username = User.Identity.Name ?? "Sin nombre",

                    // Al asignar PermisosJson, el setter del DTO (con la limpieza que pusimos)
                    // convertirá el string escapado en el diccionario real de permisos.
                    PermisosJson = permisosRaw ?? "{}"
                };

                return usuario;
            }

            // Fallback: Si no hay token o no está autenticado, devolvemos un usuario anónimo sin permisos
            // Esto evita errores de referencia nula en los controladores.
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