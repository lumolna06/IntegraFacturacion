using Microsoft.AspNetCore.Mvc;
using IntegraPro.AppLogic.Services;
using IntegraPro.DTO.Models;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompraController(CompraService compraService) : ControllerBase
{
    private readonly CompraService _service = compraService;

    [HttpPost]
    public IActionResult Post(CompraDTO compra)
    {
        try
        {
            _service.RegistrarNuevaCompra(compra);
            return Ok(new { mensaje = "Compra procesada exitosamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}/{usuarioId}")]
    public IActionResult Anular(int id, int usuarioId)
    {
        try
        {
            _service.AnularCompraExistente(id, usuarioId);
            return Ok(new { mensaje = "Compra anulada y stock revertido." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // --- NUEVO ENDPOINT DE ALERTAS ---
    [HttpGet("alertas-pagos")]
    public IActionResult GetAlertas()
    {
        try
        {
            var alertas = _service.ListarAlertasPagos();
            return Ok(alertas);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}