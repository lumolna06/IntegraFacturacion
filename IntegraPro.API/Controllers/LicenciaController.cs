using IntegraPro.AppLogic.Services;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntegraPro.API.Controllers; // Ajustado al namespace de tus otros controllers

[ApiController]
[Route("api/[controller]")]
public class LicenciaController(LicenciaService service) : BaseController // Herencia aplicada
{
    private readonly LicenciaService _service = service;

    [AllowAnonymous]
    [HttpGet("mi-hardware-id")]
    public IActionResult GetHardwareId() => Ok(new { hardwareId = _service.GetHardwareId() });

    [AllowAnonymous]
    [HttpPost("activar")]
    public IActionResult Activar([FromQuery] string llave, [FromQuery] string empresa, [FromQuery] string ruc, [FromQuery] int maxEquipos)
    {
        // Usamos el sistema de permisos de UsuarioDTO pero marcado como Sistema (ID 0)
        // ya que en la activación inicial no hay un token JWT todavía.
        var sistema = new UsuarioDTO { Id = 0, RolId = 1, SucursalId = 1, PermisosJson = "{\"config\":true}" };

        var response = _service.ActivarLicencia(llave, empresa, ruc, maxEquipos, sistema);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("auto-activar")]
    public IActionResult AutoActivar([FromQuery] string empresa, [FromQuery] string ruc)
    {
        try
        {
            var sistema = new UsuarioDTO { Id = 0, RolId = 1, SucursalId = 1, PermisosJson = "{\"config\":true}" };

            string hid = _service.GetHardwareId();
            int equiposGratis = 5;
            string llaveGenerada = _service.GenerarLlave(ruc, hid, equiposGratis);

            var response = _service.ActivarLicencia(llaveGenerada, empresa, ruc, equiposGratis, sistema);
            return Ok(response);
        }
        catch (Exception ex) { return BadRequest(new ApiResponse<bool>(false, ex.Message)); }
    }

    [HttpGet("validar-estado")]
    [Authorize] // Para validar el estado actual, sí exigimos que el usuario esté logueado
    public IActionResult Validar()
    {
        // Aquí podrías incluso pasar UsuarioActual si el servicio lo requiere para auditoría
        var response = _service.ValidarSistema();
        return response.Result ? Ok(response) : StatusCode(403, response);
    }
}