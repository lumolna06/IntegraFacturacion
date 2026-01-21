using IntegraPro.AppLogic.Services;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
// Usamos el constructor primario para inyectar el servicio directamente
public class VentaController(VentaService service) : ControllerBase
{
    private readonly VentaService _service = service;

    [HttpPost]
    public IActionResult Post([FromBody] FacturaDTO factura)
    {
        try
        {
            // El servicio orquestará la transacción: Factura + Kardex + Trigger
            string numFac = _service.ProcesarVenta(factura);

            return Ok(new
            {
                success = true,
                factura = numFac,
                message = "Venta procesada exitosamente. El inventario se actualizó mediante Kardex y Trigger."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}