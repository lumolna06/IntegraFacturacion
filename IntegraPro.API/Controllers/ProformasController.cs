using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Asegura que nadie entre sin Token
public class ProformasController(
    IProformaService service,
    IConfiguracionService configService) : BaseController // HERENCIA: Ahora hereda de tu BaseController
{
    private readonly IProformaService _service = service;
    private readonly IConfiguracionService _configService = configService;

    [HttpPost]
    public IActionResult Post(ProformaEncabezadoDTO dto)
    {
        // CAMBIO: Usamos UsuarioActual (heredado)
        var res = _service.GuardarProforma(dto, UsuarioActual);
        return res.Result ? Ok(res) : TratarResultadoFallido(res);
    }

    [HttpGet]
    public IActionResult Get(string filtro = "")
    {
        var res = _service.Consultar(filtro, UsuarioActual);
        return Ok(res);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var res = _service.ObtenerPorId(id, UsuarioActual);
        return res.Result ? Ok(res) : NotFound(res);
    }

    [HttpGet("{id}/imprimir")]
    public IActionResult GenerarPdf(int id)
    {
        try
        {
            // CAMBIO: Usamos UsuarioActual
            var resProforma = _service.ObtenerPorId(id, UsuarioActual);
            var resEmpresa = _configService.ObtenerDatosEmpresa(UsuarioActual);

            if (!resProforma.Result || resProforma.Data == null) return NotFound(resProforma);
            if (!resEmpresa.Result || resEmpresa.Data == null) return BadRequest(resEmpresa);

            QuestPDF.Settings.License = LicenseType.Community;

            var documento = new Reports.ProformaReport(resProforma.Data, resEmpresa.Data);
            byte[] pdfBytes = documento.GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Proforma_{id}_{DateTime.Now:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(false, "Error al generar reporte: " + ex.Message));
        }
    }

    [HttpGet("cliente/{clienteId}")]
    public IActionResult GetByCliente(int clienteId)
    {
        var res = _service.ObtenerPorCliente(clienteId, UsuarioActual);
        return Ok(res);
    }

    [HttpPatch("{id}/anular")]
    public IActionResult Anular(int id)
    {
        var res = _service.Anular(id, UsuarioActual);
        return res.Result ? Ok(res) : TratarResultadoFallido(res);
    }

    [HttpPost("{id}/convertir-a-factura")]
    public IActionResult Facturar(int id, [FromQuery] string medio = "Efectivo")
    {
        var res = _service.Facturar(id, medio, UsuarioActual);
        return res.Result ? Ok(res) : TratarResultadoFallido(res);
    }

    private IActionResult TratarResultadoFallido<T>(ApiResponse<T> res)
    {
        if (res.Message.Contains("denegado") || res.Message.Contains("No tiene permiso"))
            return StatusCode(403, res);

        return BadRequest(res);
    }

    // NOTA: Se borró ObtenerEjecutor() porque BaseController ya lo hace mejor.
}