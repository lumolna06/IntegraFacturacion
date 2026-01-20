using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public AuthController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpPost("login")]
    public ActionResult<ApiResponse<UsuarioDTO>> Login([FromBody] LoginRequest request)
    {
        var response = _usuarioService.Login(request.Username, request.Password);
        if (!response.Result)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("register")]
    public ActionResult<ApiResponse<bool>> Register([FromBody] UsuarioDTO usuario)
    {
        var response = _usuarioService.Registrar(usuario);
        if (!response.Result)
            return BadRequest(response);

        return Ok(response);
    }
}

public record LoginRequest(string Username, string Password);