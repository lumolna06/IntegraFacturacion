using Microsoft.AspNetCore.Mvc;
using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AbonosController(IAbonoService service) : ControllerBase
{
    private readonly IAbonoService _service = service;

    [HttpGet("buscar")]
    public IActionResult Buscar([FromQuery] string? filtro = null) // SE CORRIGIÓ: string? y = null
    {
        // Al ser opcional, si no envías nada en la URL, llegará como null
        // y tu Factory ejecutará la consulta sin filtros (traerá todo).
        var res = _service.BuscarCuentas(filtro, ObtenerEjecutor());
        return res.Result ? Ok(res) : BadRequest(res);
    }

    [HttpGet("dashboard/resumen")]
    public IActionResult Resumen()
    {
        var res = _service.ObtenerResumenGeneral(ObtenerEjecutor());
        return res.Result ? Ok(res) : BadRequest(res);
    }

    [HttpPost("registrar/{clienteId}")]
    public IActionResult Registrar(int clienteId, [FromBody] AbonoDTO abono)
    {
        var res = _service.ProcesarAbono(abono, clienteId, ObtenerEjecutor());
        return res.Result ? Ok(res) : BadRequest(res);
    }

    [HttpGet("historial/{cuentaId}")]
    public IActionResult Historial(int cuentaId)
    {
        var res = _service.ObtenerHistorial(cuentaId);
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