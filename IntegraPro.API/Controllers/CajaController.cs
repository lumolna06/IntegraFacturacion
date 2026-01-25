using Microsoft.AspNetCore.Mvc;
using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CajaController(ICajaService service) : ControllerBase
{
    private readonly ICajaService _service = service;

    [HttpPost("abrir")]
    public IActionResult Abrir([FromBody] CajaAperturaDTO apertura)
    {
        var res = _service.AbrirCaja(apertura, ObtenerEjecutor());
        return res.Result ? Ok(res) : BadRequest(res);
    }

    [HttpPost("cerrar")]
    public IActionResult Cerrar([FromBody] CajaCierreDTO cierre)
    {
        var res = _service.CerrarCaja(cierre, ObtenerEjecutor());
        return res.Result ? Ok(res) : BadRequest(res);
    }

    [HttpGet("historial")]
    public IActionResult GetHistorial()
    {
        var res = _service.ObtenerHistorial(ObtenerEjecutor());
        return res.Result ? Ok(res) : BadRequest(res);
    }

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
        return new UsuarioDTO { Id = 3, RolId = 1, SucursalId = 1 };
    }
}