using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // Necesario para [Authorize]
using System.Security.Claims;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // <--- MEDIDA DE SEGURIDAD 1: Todo el controlador requiere JWT por defecto
public class UsuarioController(IUsuarioService service) : ControllerBase
{
    private readonly IUsuarioService _service = service;

    [HttpPost("login")]
    [AllowAnonymous] // <--- MEDIDA DE SEGURIDAD 2: El login es la única puerta abierta
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var result = _service.Login(request.Username, request.Password);
        if (!result.Result) return Unauthorized(result);
        return Ok(result);
    }

    [HttpPost("registrar")]
    public IActionResult Registrar([FromBody] UsuarioDTO usuario)
    {
        var result = _service.Registrar(usuario, ObtenerEjecutor());
        if (!result.Result) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("logout/{id}")]
    public IActionResult Logout(int id)
    {
        // Validamos que el usuario solo pueda cerrarse la sesión a sí mismo 
        // a menos que sea un administrador.
        var result = _service.Logout(id);
        return Ok(result);
    }

    [HttpPost("forzar-cierre")]
    [AllowAnonymous] // Se permite anónimo para que el usuario libere su propia sesión si se trabó
    public IActionResult ForzarCierre([FromBody] LoginRequest request)
    {
        var result = _service.ForzarCierreSesion(request.Username, request.Password);
        if (!result.Result) return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("listar-todos")]
    public IActionResult GetAll()
    {
        var result = _service.ObtenerTodos(ObtenerEjecutor());
        return Ok(result);
    }

    [HttpGet("roles-disponibles")]
    public IActionResult GetRoles()
    {
        var result = _service.ListarRolesDisponibles(ObtenerEjecutor());
        return Ok(result);
    }

    [HttpPut("actualizar-rol")]
    public IActionResult UpdateRol([FromQuery] int usuarioId, [FromQuery] int nuevoRolId)
    {
        var result = _service.ActualizarRol(usuarioId, nuevoRolId, ObtenerEjecutor());
        if (!result.Result) return BadRequest(result);
        return Ok(result);
    }

    private UsuarioDTO ObtenerEjecutor()
    {
        // MEDIDA DE SEGURIDAD 3: Sincronización exacta con los claims del JWT generado en el Service
        if (User.Identity?.IsAuthenticated == true)
        {
            return new UsuarioDTO
            {
                // Usamos los strings exactos que definimos en UsuarioService.GenerarJwtToken
                Id = int.Parse(User.FindFirst("id")?.Value ?? "0"),
                RolId = int.Parse(User.FindFirst("rolId")?.Value ?? "0"),
                SucursalId = int.Parse(User.FindFirst("sucursalId")?.Value ?? "0"),
                Username = User.Identity.Name ?? string.Empty,
                PermisosJson = User.FindFirst("permisos")?.Value
            };
        }

        // Ya no devolvemos un Admin de prueba. Si llegamos aquí, es un error de seguridad.
        throw new UnauthorizedAccessException("Operación no permitida sin una sesión válida.");
    }
}