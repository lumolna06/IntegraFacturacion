using Microsoft.AspNetCore.Mvc;
using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Authorization;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Bloquea el acceso si no hay un token JWT válido
public class CajaController(ICajaService service) : BaseController // Herencia aplicada
{
    private readonly ICajaService _service = service;

    [HttpPost("abrir")]
    public IActionResult Abrir([FromBody] CajaAperturaDTO apertura)
    {
        // Usamos la propiedad UsuarioActual de BaseController
        var res = _service.AbrirCaja(apertura, UsuarioActual);
        return res.Result ? Ok(res) : BadRequest(res);
    }

    [HttpPost("cerrar")]
    public IActionResult Cerrar([FromBody] CajaCierreDTO cierre)
    {
        var res = _service.CerrarCaja(cierre, UsuarioActual);
        return res.Result ? Ok(res) : BadRequest(res);
    }

    [HttpGet("historial")]
    public IActionResult GetHistorial()
    {
        var res = _service.ObtenerHistorial(UsuarioActual);
        return res.Result ? Ok(res) : BadRequest(res);
    }
}