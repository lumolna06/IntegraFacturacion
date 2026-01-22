using IntegraPro.AppLogic.Services;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using System;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProformasController(ProformaService service) : ControllerBase
{
    // Crear una nueva proforma
    [HttpPost]
    public IActionResult Post(ProformaEncabezadoDTO dto)
        => Ok(new { id = service.GuardarProforma(dto), message = "Proforma registrada exitosamente" });

    // Listado general con filtro opcional
    [HttpGet]
    public IActionResult Get(string filtro = "")
        => Ok(service.Consultar(filtro));

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

    // Convertir proforma a factura (activa los triggers de inventario y CxC)
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