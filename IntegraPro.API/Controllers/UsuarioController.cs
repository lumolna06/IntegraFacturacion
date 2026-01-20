using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuarioController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpPost("login")]
    public ActionResult<ApiResponse<UsuarioDTO>> Login([FromBody] LoginRequest request)
    {
        var result = _usuarioService.Login(request.Username, request.Password);

        // Cambiado de .Success a .Result
        if (result.Result)
        {
            return Ok(result);
        }

        if (result.Message == "SESION_ABIERTA_OTRO_EQUIPO")
        {
            return Conflict(result);
        }

        return BadRequest(result);
    }

    [HttpPost("forzar-cierre-sesion")]
    public ActionResult<ApiResponse<bool>> ForzarCierre([FromBody] LoginRequest request)
    {
        var result = _usuarioService.ForzarCierreSesion(request.Username, request.Password);

        if (result.Result) // Cambiado a .Result
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpPost("registrar")]
    public ActionResult<ApiResponse<bool>> Registrar([FromBody] UsuarioDTO usuario)
    {
        var result = _usuarioService.Registrar(usuario);

        if (result.Result) // Cambiado a .Result
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpPost("logout/{id}")]
    public ActionResult<ApiResponse<bool>> Logout(int id)
    {
        var result = _usuarioService.Logout(id);

        if (result.Result) // Cambiado a .Result
        {
            return Ok(result);
        }

        return BadRequest(result);
    }
}