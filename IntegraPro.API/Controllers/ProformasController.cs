using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProformasController(
    IProformaService service,
    IConfiguracionService configService) : ControllerBase
{
    private readonly IProformaService _service = service;
    private readonly IConfiguracionService _configService = configService;

    [HttpPost]
    public IActionResult Post(ProformaEncabezadoDTO dto)
    {
        var res = _service.GuardarProforma(dto, ObtenerEjecutor());
        return res.Result ? Ok(res) : BadRequest(res);
    }

    [HttpGet]
    public IActionResult Get(string filtro = "")
    {
        var res = _service.Consultar(filtro, ObtenerEjecutor());
        return Ok(res);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var res = _service.ObtenerPorId(id, ObtenerEjecutor());
        return res.Result ? Ok(res) : NotFound(res);
    }

    [HttpGet("{id}/imprimir")]
    public IActionResult GenerarPdf(int id)
    {
        try
        {
            var ejecutor = ObtenerEjecutor();
            var resProforma = _service.ObtenerPorId(id, ejecutor);
            var resEmpresa = _configService.ObtenerDatosEmpresa();

            if (!resProforma.Result || resProforma.Data == null) return NotFound(resProforma);
            if (!resEmpresa.Result || resEmpresa.Data == null) return BadRequest(resEmpresa);

            QuestPDF.Settings.License = LicenseType.Community;
            var documento = new Reports.ProformaReport(resProforma.Data, resEmpresa.Data);
            byte[] pdfBytes = documento.GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Proforma_{id}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Error en reporte: " + ex.Message });
        }
    }

    [HttpGet("cliente/{clienteId}")]
    public IActionResult GetByCliente(int clienteId)
    {
        var res = _service.ObtenerPorCliente(clienteId, ObtenerEjecutor());
        return Ok(res);
    }

    [HttpPatch("{id}/anular")]
    public IActionResult Anular(int id)
    {
        var res = _service.Anular(id, ObtenerEjecutor());
        return res.Result ? Ok(res) : BadRequest(res);
    }

    [HttpPost("{id}/convertir-a-factura")]
    public IActionResult Facturar(int id, [FromQuery] string medio = "Efectivo")
    {
        var res = _service.Facturar(id, medio, ObtenerEjecutor());
        return res.Result ? Ok(res) : BadRequest(res);
    }

    private UsuarioDTO ObtenerEjecutor()
    {
        // En producción, extraer de Claims JWT
        return new UsuarioDTO
        {
            Id = 3,
            RolId = 1,
            SucursalId = 1,
            Permisos = new Dictionary<string, bool> { { "proformas", true }, { "ventas", false }, { "config", true } }
        };
    }
}