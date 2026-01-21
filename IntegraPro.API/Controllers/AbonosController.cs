using Microsoft.AspNetCore.Mvc;
using IntegraPro.AppLogic.Services;
using IntegraPro.DTO.Models;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AbonosController(IConfiguration config) : ControllerBase
{
    private readonly AbonoService _abonoService = new(config.GetConnectionString("DefaultConnection")!);

    // --- NUEVO MÉTODO PARA CONSULTAR FACTURAS PENDIENTES ---
    // Se puede llamar como: api/Abonos/Pendientes o api/Abonos/Pendientes?clienteId=1
    [HttpGet("Pendientes")]
    public IActionResult GetPendientes([FromQuery] int? clienteId)
    {
        try
        {
            var pendientes = _abonoService.ObtenerPendientes(clienteId);
            return Ok(new { success = true, data = pendientes });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("RegistrarAbono/{clienteId}")]
    public IActionResult Post([FromBody] AbonoDTO abono, int clienteId)
    {
        try
        {
            _abonoService.RegistrarAbono(abono, clienteId);
            return Ok(new { success = true, message = "Abono procesado correctamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}