using Microsoft.AspNetCore.Mvc;
using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Authorization;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Protege todo el módulo de compras
public class CompraController(ICompraService compraService) : BaseController // Hereda de tu BaseController real
{
    private readonly ICompraService _service = compraService;

    [HttpPost]
    public IActionResult Post(CompraDTO compra)
    {
        try
        {
            // Usamos UsuarioActual del BaseController (ya trae claims y permisos)
            _service.RegistrarNuevaCompra(compra, UsuarioActual);
            return Ok(new { mensaje = "Compra procesada exitosamente." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message, detalle = "Acceso denegado al módulo de compras." });
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
            _service.AbonarAFactura(pago, UsuarioActual);
            return Ok(new { mensaje = "Pago registrado exitosamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("resumen-general")]
    public IActionResult GetResumenGeneral()
    {
        try
        {
            var resumen = _service.ObtenerResumenCxp(UsuarioActual);
            return Ok(resumen);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("buscar-deudas")]
    public IActionResult GetDeudas([FromQuery] string? filtro = null)
    {
        try
        {
            var deudas = _service.ListarDeudas(filtro ?? "", UsuarioActual);
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
            // Nota: Aquí podrías pasar UsuarioActual si el service requiere filtrar por sucursal
            var historial = _service.ListarHistorialDePagos(compraId);
            return Ok(historial);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public IActionResult Anular(int id)
    {
        try
        {
            _service.AnularCompraExistente(id, UsuarioActual);
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
            var alertas = _service.ListarAlertasPagos(UsuarioActual);
            return Ok(alertas);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}