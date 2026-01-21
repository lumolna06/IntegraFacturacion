using Microsoft.AspNetCore.Mvc;
using IntegraPro.AppLogic.Services;
using IntegraPro.DTO.Models;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
// Cambiamos el constructor para inyectar el CajaService directamente
public class CajaController(CajaService cajaService) : ControllerBase
{
    // Ya no usamos IConfiguration ni 'new', el sistema nos da el servicio listo
    private readonly CajaService _cajaService = cajaService;

    [HttpPost("Abrir")]
    public IActionResult Abrir([FromBody] CajaAperturaDTO apertura)
    {
        try
        {
            int id = _cajaService.AbrirCaja(apertura);
            return Ok(new { success = true, cajaId = id, message = "Caja abierta correctamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("Cerrar")]
    public IActionResult Cerrar([FromBody] CajaCierreDTO cierre)
    {
        try
        {
            _cajaService.CerrarCaja(cierre);
            return Ok(new { success = true, message = "Caja cerrada y liquidada exitosamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("Historial")]
    public IActionResult GetHistorial()
    {
        try
        {
            var historial = _cajaService.ObtenerHistorial();
            return Ok(new { success = true, data = historial });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}