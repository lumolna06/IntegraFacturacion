using Microsoft.AspNetCore.Mvc;
using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Authorization;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Importante: Asegura que el Token sea obligatorio
public class AbonosController(IAbonoService service) : BaseController // Hereda de tu BaseController
{
    private readonly IAbonoService _service = service;

    [HttpGet("buscar")]
    public IActionResult Buscar([FromQuery] string? filtro = null)
    {
        // Usamos UsuarioActual (la propiedad real de tu BaseController)
        var res = _service.BuscarCuentas(filtro, UsuarioActual);
        return res.Result ? Ok(res) : BadRequest(res);
    }

    [HttpGet("dashboard/resumen")]
    public IActionResult Resumen()
    {
        var res = _service.ObtenerResumenGeneral(UsuarioActual);
        return res.Result ? Ok(res) : BadRequest(res);
    }

    [HttpPost("registrar/{clienteId}")]
    public IActionResult Registrar(int clienteId, [FromBody] AbonoDTO abono)
    {
        var res = _service.ProcesarAbono(abono, clienteId, UsuarioActual);
        return res.Result ? Ok(res) : BadRequest(res);
    }

    [HttpGet("historial/{cuentaId}")]
    public IActionResult Historial(int cuentaId)
    {
        var res = _service.ObtenerHistorial(cuentaId, UsuarioActual);
        return res.Result ? Ok(res) : BadRequest(res);
    }
}