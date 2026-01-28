using IntegraPro.AppLogic.Services;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Excluye este controlador del LicenseFilter global a nivel de clase
public class LicenciaController(LicenciaService service) : BaseController
{
    private readonly LicenciaService _service = service;

    [HttpGet("mi-hardware-id")]
    public IActionResult GetHardwareId() => Ok(new { hardwareId = _service.GetHardwareId() });

    [HttpPost("activar")]
    public IActionResult Activar([FromQuery] string llave = "", [FromQuery] string ruc = "", [FromQuery] int maxEquipos = 1)
    {
        // Validamos manualmente para evitar el Error 400 automático de .NET
        if (string.IsNullOrWhiteSpace(llave) || string.IsNullOrWhiteSpace(ruc))
        {
            return Ok(new ApiResponse<bool>(false, "La llave de activación y la identificación son obligatorias."));
        }

        var response = _service.ActivarLicencia(llave.Trim(), ruc.Trim(), maxEquipos);

        // Siempre devolvemos Ok(200) para que el AJAX de jQuery pueda leer la respuesta
        // El 'toastr' se encargará de mostrar si fue true o false según res.result
        return Ok(response);
    }

    [HttpPost("auto-activar")]
    public IActionResult AutoActivar([FromQuery] string ruc = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ruc))
                return Ok(new ApiResponse<bool>(false, "El RUC es obligatorio para la auto-activación."));

            string hid = _service.GetHardwareId();
            int equiposGratis = 5;

            string llaveGenerada = _service.GenerarLlave(ruc.Trim(), hid, equiposGratis);

            var response = _service.ActivarLicencia(llaveGenerada, ruc.Trim(), equiposGratis);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Ok(new ApiResponse<bool>(false, $"Error interno: {ex.Message}"));
        }
    }

    [HttpGet("validar-estado")]
    [Authorize] // Este se mantiene protegido ya que requiere un token de usuario logueado
    public IActionResult Validar()
    {
        var response = _service.ValidarSistema();
        return response.Result ? Ok(response) : StatusCode(403, response);
    }
}