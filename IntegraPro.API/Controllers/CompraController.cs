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

    [HttpPost("pagar")]
    public IActionResult PagarFactura([FromBody] PagoCxpDTO pago)
    {
        try
        {
            _service.AbonarAFactura(pago);
            return Ok(new { mensaje = "Pago registrado exitosamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // --- NUEVO ENDPOINT: RESUMEN GLOBAL PARA DASHBOARD ---
    [HttpGet("resumen-general")]
    public IActionResult GetResumenGeneral()
    {
        try
        {
            var resumen = _service.ObtenerResumenCxp();
            return Ok(resumen);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("buscar-deudas")]
    public IActionResult GetDeudas([FromQuery] string filtro)
    {
        try
        {
            var deudas = _service.ListarDeudas(filtro ?? "");
            return Ok(deudas);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{compraId}/historial-pagos")]
    public IActionResult GetHistorialPagos(int compraId)
    {
        try
        {
            var historial = _service.ListarHistorialDePagos(compraId);
            return Ok(historial);
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