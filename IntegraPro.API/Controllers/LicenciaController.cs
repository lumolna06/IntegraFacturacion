using IntegraPro.AppLogic.Services;
using IntegraPro.AppLogic.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntegraPro.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicenciaController : ControllerBase
{
    private readonly LicenciaService _service;

    public LicenciaController(LicenciaService service)
    {
        _service = service;
    }

    /// <summary>
    /// Paso 1: Obtener el Identificador de Hardware.
    /// Accesible sin licencia para permitir el inicio del proceso.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("mi-hardware-id")]
    public IActionResult GetHardwareId()
    {
        string hid = _service.GetHardwareId();
        return Ok(new { hardwareId = hid });
    }

    /// <summary>
    /// Paso 2 (Manual): Activación mediante una llave generada por el administrador.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("activar")]
    public IActionResult Activar([FromQuery] string llave, [FromQuery] string empresa, [FromQuery] string ruc, [FromQuery] int maxEquipos)
    {
        var response = _service.ActivarLicencia(llave, empresa, ruc, maxEquipos);
        return Ok(response);
    }

    /// <summary>
    /// Paso 2 (Automático): El sistema se activa solo con datos básicos.
    /// Genera la llave internamente para que el usuario no dependa del administrador inicialmente.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("auto-activar")]
    public IActionResult AutoActivar([FromQuery] string empresa, [FromQuery] string ruc)
    {
        try
        {
            string hid = _service.GetHardwareId();
            // Generamos la llave automáticamente para una licencia estándar de 5 equipos
            int equiposGratis = 5;
            string llaveGenerada = _service.GenerarLlave(ruc, hid, equiposGratis);

            var response = _service.ActivarLicencia(llaveGenerada, empresa, ruc, equiposGratis);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<bool>(false, $"Error en auto-activación: {ex.Message}"));
        }
    }

    /// <summary>
    /// Verifica si el sistema está activo y si el RUC no ha sido bloqueado remotamente.
    /// </summary>
    [HttpGet("validar-estado")]
    public IActionResult Validar()
    {
        var response = _service.ValidarSistema();
        // Si el RUC está en tu lista de GitHub, ValidarSistema() devolverá false.
        return response.Result ? Ok(response) : StatusCode(403, response);
    }
}