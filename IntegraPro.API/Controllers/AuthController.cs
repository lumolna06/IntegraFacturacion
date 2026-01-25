using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// Heredamos de BaseController para usar 'UsuarioActual' en el registro
public class AuthController(IUsuarioService usuarioService) : BaseController
{
    private readonly IUsuarioService _usuarioService = usuarioService;

    [HttpPost("login")]
    [AllowAnonymous] // El login SIEMPRE debe ser público
    public ActionResult<ApiResponse<UsuarioDTO>> Login([FromBody] LoginRequest request)
    {
        // El Login no usa UsuarioActual porque el usuario apenas se está identificando
        var response = _usuarioService.Login(request.Username, request.Password);

        if (!response.Result)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("register")]
    [Authorize] // Solo usuarios autenticados (Admin) pueden registrar a otros
    public ActionResult<ApiResponse<bool>> Register([FromBody] UsuarioDTO usuario)
    {
        // Eliminamos ObtenerEjecutor() local y usamos UsuarioActual del BaseController
        // Esto garantiza que el registro quede auditado con el ID del Admin real
        var response = _usuarioService.Registrar(usuario, UsuarioActual);

        if (!response.Result)
            return BadRequest(response);

        return Ok(response);
    }
}

public record LoginRequest(string Username, string Password);