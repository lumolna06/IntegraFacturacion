using IntegraPro.AppLogic.Services;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;      // Necesario para .GeneratePdf()
using QuestPDF.Infrastructure; // Necesario para LicenseType
using System;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProformasController(ProformaService service) : ControllerBase
{
    // Crear una nueva proforma
    [HttpPost]
    public IActionResult Post(ProformaEncabezadoDTO dto)
    {
        try
        {
            var id = service.GuardarProforma(dto);
            return Ok(new { id, message = "Proforma registrada exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // Listado general con filtro opcional
    [HttpGet]
    public IActionResult Get(string filtro = "")
        => Ok(service.Consultar(filtro));

    // OBTENER UNA SOLA PROFORMA
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var proforma = service.ObtenerPorId(id);
        if (proforma == null) return NotFound(new { message = "La proforma solicitada no existe" });
        return Ok(proforma);
    }

    // --- ENDPOINT PARA IMPRIMIR PDF (CORREGIDO) ---
    [HttpGet("{id}/imprimir")]
    public IActionResult GenerarPdf(int id)
    {
        try
        {
            // 1. Obtener los datos de la proforma
            var proforma = service.ObtenerPorId(id);
            if (proforma == null) return NotFound(new { message = "Proforma no encontrada" });

            // 2. Obtener los datos de la empresa (Usa el nuevo método del Service)
            var datosEmpresa = service.ObtenerEmpresa();
            if (datosEmpresa == null) return BadRequest(new { message = "Debe configurar los datos de la empresa primero." });

            // Configurar licencia para QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;

            // 3. Instanciar el reporte pasando AMBOS objetos para cumplir con el constructor
            var documento = new Reports.ProformaReport(proforma, datosEmpresa);
            byte[] pdfBytes = documento.GeneratePdf();

            // Retornar el archivo PDF para abrir o descargar
            return File(pdfBytes, "application/pdf", $"Proforma_{id}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Error al generar el reporte: " + ex.Message });
        }
    }

    // Buscar proformas de un cliente específico
    [HttpGet("cliente/{clienteId}")]
    public IActionResult GetByCliente(int clienteId)
        => Ok(service.ObtenerPorCliente(clienteId));

    // Editar una proforma existente
    [HttpPut("{id}")]
    public IActionResult Put(int id, ProformaEncabezadoDTO dto)
    {
        try
        {
            dto.Id = id;
            service.EditarProforma(dto);
            return Ok(new { success = true, message = "Proforma actualizada exitosamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // ANULAR PROFORMA
    [HttpPatch("{id}/anular")]
    public IActionResult Anular(int id)
    {
        try
        {
            service.Anular(id);
            return Ok(new { success = true, message = "Proforma anulada correctamente" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // Convertir proforma a factura
    [HttpPost("{id}/convertir-a-factura")]
    public IActionResult Facturar(int id, [FromQuery] int usuarioId, [FromQuery] string medio = "Efectivo")
    {
        try
        {
            var nDoc = service.Facturar(id, usuarioId, medio);
            return Ok(new { success = true, numeroFactura = nDoc });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}