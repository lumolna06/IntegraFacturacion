using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;

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

        // Ajustado de .Success a .Result para coincidir con tu Utils.ApiResponse
        if (!result.Result) return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("registrar")]
    public IActionResult Registrar([FromBody] UsuarioDTO usuario)
    {
        var result = _service.Registrar(usuario);

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
    // NUEVOS: ENDPOINTS DE ADMINISTRACIÓN
    // ==========================================

    [HttpGet("listar-todos")]
    public IActionResult GetAll()
    {
        var result = _service.ObtenerTodos();
        return Ok(result);
    }

    [HttpGet("roles-disponibles")]
    public IActionResult GetRoles()
    {
        var result = _service.ListarRolesDisponibles();
        return Ok(result);
    }

    [HttpPut("actualizar-rol")]
    public IActionResult UpdateRol([FromQuery] int usuarioId, [FromQuery] int nuevoRolId)
    {
        var result = _service.ActualizarRol(usuarioId, nuevoRolId);

        if (!result.Result) return BadRequest(result);

        return Ok(result);
    }
}