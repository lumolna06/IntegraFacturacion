using IntegraPro.AppLogic.Services;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntegraPro.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicenciaController(LicenciaService service) : ControllerBase
{
    private readonly LicenciaService _service = service;

    [AllowAnonymous]
    [HttpGet("mi-hardware-id")]
    public IActionResult GetHardwareId() => Ok(new { hardwareId = _service.GetHardwareId() });

    [AllowAnonymous]
    [HttpPost("activar")]
    public IActionResult Activar([FromQuery] string llave, [FromQuery] string empresa, [FromQuery] string ruc, [FromQuery] int maxEquipos)
    {
        var response = _service.ActivarLicencia(llave, empresa, ruc, maxEquipos, ObtenerUsuarioSistema());
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("auto-activar")]
    public IActionResult AutoActivar([FromQuery] string empresa, [FromQuery] string ruc)
    {
        try
        {
            string hid = _service.GetHardwareId();
            int equiposGratis = 5;
            string llaveGenerada = _service.GenerarLlave(ruc, hid, equiposGratis);
            var response = _service.ActivarLicencia(llaveGenerada, empresa, ruc, equiposGratis, ObtenerUsuarioSistema());
            return Ok(response);
        }
        catch (Exception ex) { return BadRequest(new ApiResponse<bool>(false, ex.Message)); }
    }

    [HttpGet("validar-estado")]
    public IActionResult Validar()
    {
        var response = _service.ValidarSistema();
        return response.Result ? Ok(response) : StatusCode(403, response);
    }

    private UsuarioDTO ObtenerUsuarioSistema()
    {
        return new UsuarioDTO
        {
            Id = 0,
            RolId = 1,
            SucursalId = 1,
            // Solución CS0029: Usar Dictionary en lugar de List
            Permisos = new Dictionary<string, bool> { { "config", true } }
        };
    }
}