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
        // El Login no requiere ejecutor porque el usuario aún no se ha autenticado
        var response = _usuarioService.Login(request.Username, request.Password);
        if (!response.Result)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("register")]
    public ActionResult<ApiResponse<bool>> Register([FromBody] UsuarioDTO usuario)
    {
        // ERROR CORREGIDO: Ahora pasamos el ejecutor. 
        // Si el registro es abierto (público), pasamos un objeto temporal o el mismo usuario.
        // Si es administrativo, usamos ObtenerEjecutor().

        var ejecutor = ObtenerEjecutor();
        var response = _usuarioService.Registrar(usuario, ejecutor);

        if (!response.Result)
            return BadRequest(response);

        return Ok(response);
    }

    /// <summary>
    /// Extrae el usuario que realiza la petición.
    /// </summary>
    private UsuarioDTO ObtenerEjecutor()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return new UsuarioDTO
            {
                Id = int.Parse(User.FindFirst("id")?.Value ?? "0"),
                RolId = int.Parse(User.FindFirst("rolId")?.Value ?? "0"),
                SucursalId = int.Parse(User.FindFirst("sucursalId")?.Value ?? "0")
            };
        }
        // Usuario por defecto para permitir el primer registro (Admin) o si no hay login aún
        return new UsuarioDTO { Id = 0, RolId = 1, SucursalId = 1 };
    }
}

public record LoginRequest(string Username, string Password);