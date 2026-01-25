using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsuarioController(IUsuarioService service) : ControllerBase
{
    private readonly IUsuarioService _service = service;

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var result = _service.Login(request.Username, request.Password);

        if (!result.Result) return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("registrar")]
    public IActionResult Registrar([FromBody] UsuarioDTO usuario)
    {
        // CORRECCIÓN: Se pasa el ejecutor
        var result = _service.Registrar(usuario, ObtenerEjecutor());

        if (!result.Result) return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("logout/{id}")]
    public IActionResult Logout(int id)
    {
        var result = _service.Logout(id);
        return Ok(result);
    }

    [HttpPost("forzar-cierre")]
    public IActionResult ForzarCierre([FromBody] LoginRequest request)
    {
        var result = _service.ForzarCierreSesion(request.Username, request.Password);

        if (!result.Result) return BadRequest(result);

        return Ok(result);
    }

    // ==========================================
    // ENDPOINTS DE ADMINISTRACIÓN
    // ==========================================

    [HttpGet("listar-todos")]
    public IActionResult GetAll()
    {
        // CORRECCIÓN: Se pasa el ejecutor
        var result = _service.ObtenerTodos(ObtenerEjecutor());
        return Ok(result);
    }

    [HttpGet("roles-disponibles")]
    public IActionResult GetRoles()
    {
        // CORRECCIÓN: Se pasa el ejecutor
        var result = _service.ListarRolesDisponibles(ObtenerEjecutor());
        return Ok(result);
    }

    [HttpPut("actualizar-rol")]
    public IActionResult UpdateRol([FromQuery] int usuarioId, [FromQuery] int nuevoRolId)
    {
        // CORRECCIÓN: Se pasa el ejecutor
        var result = _service.ActualizarRol(usuarioId, nuevoRolId, ObtenerEjecutor());

        if (!result.Result) return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Método privado para obtener los datos del usuario que está operando.
    /// Esto resuelve los errores de "No se ha dado ningún argumento que corresponda al parámetro ejecutor".
    /// </summary>
    private UsuarioDTO ObtenerEjecutor()
    {
        // 1. Si el usuario está autenticado (JWT enviado)
        if (User.Identity?.IsAuthenticated == true)
        {
            return new UsuarioDTO
            {
                Id = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"),
                RolId = int.Parse(User.FindFirst("RolId")?.Value ?? "0"),
                SucursalId = int.Parse(User.FindFirst("SucursalId")?.Value ?? "0"),
                Username = User.Identity.Name ?? string.Empty,
                // IMPORTANTE: Al asignar esto, el DTO llena el diccionario automáticamente
                PermisosJson = User.FindFirst("Permisos")?.Value
            };
        }

        // 2. Fallback (Solo para pruebas iniciales si no hay token)
        // ADVERTENCIA: Una vez que el sistema esté en producción, esto debería lanzar una excepción
        return new UsuarioDTO
        {
            Id = 1,
            RolId = 1,
            SucursalId = 1,
            Username = "admin_test",
            PermisosJson = "{\"all\": true}" // Usamos el JSON para disparar la lógica del DTO
        };
    }
}