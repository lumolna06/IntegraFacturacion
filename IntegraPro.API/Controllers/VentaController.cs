using IntegraPro.AppLogic.Services;
using IntegraPro.AppLogic.Services; // Nueva ubicación
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VentaController(IConfiguration config) : ControllerBase
{
    private readonly VentaService _service = new(config.GetConnectionString("DefaultConnection")!);

    [HttpPost]
    public IActionResult Post([FromBody] FacturaDTO factura)
    {
        try
        {
            string numFac = _service.ProcesarVenta(factura);
            return Ok(new { success = true, factura = numFac, message = "Venta procesada y stock actualizado." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}