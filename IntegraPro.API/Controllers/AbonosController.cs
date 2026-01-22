using Microsoft.AspNetCore.Mvc;
using IntegraPro.AppLogic.Services;
using IntegraPro.DTO.Models;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AbonosController(AbonoService abonoService) : ControllerBase
{
    private readonly AbonoService _abonoService = abonoService;

    // 1. RESUMEN GENERAL (Para tarjetas del Dashboard)
    [HttpGet("resumen-general")]
    public IActionResult GetResumen()
    {
        try
        {
            var resumen = _abonoService.ObtenerResumenGeneral();
            return Ok(new { success = true, data = resumen });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // 2. BUSCAR CUENTAS POR COBRAR (Filtro por nombre, factura o cédula)
    [HttpGet("buscar-cxc")]
    public IActionResult BuscarCxc([FromQuery] string filtro)
    {
        try
        {
            var resultados = _abonoService.BuscarCuentasPorCobrar(filtro ?? "");
            return Ok(new { success = true, data = resultados });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // 3. ALERTAS DE MORA (Facturas ya vencidas)
    [HttpGet("alertas-mora")]
    public IActionResult GetAlertasMora()
    {
        try
        {
            var alertas = _abonoService.ListarAlertasMora();
            return Ok(new { success = true, data = alertas });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // 4. HISTORIAL DE ABONOS DE UNA FACTURA ESPECÍFICA
    [HttpGet("{cuentaId}/historial")] // <-- Swagger mostrará "cuentaId"
    public IActionResult GetHistorial(int cuentaId)
    {
        try
        {
            // Pasamos el ID al servicio (no importa que el servicio lo reciba como 'facturaId')
            var historial = _abonoService.ObtenerHistorialDeAbonos(cuentaId);
            return Ok(new { success = true, data = historial });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // 5. CONSULTAR FACTURAS PENDIENTES (Tu método original actualizado)
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

    // 6. REGISTRAR ABONO (Tu método original actualizado)
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