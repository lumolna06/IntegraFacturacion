using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using System;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracionController(ConfiguracionFactory factory) : ControllerBase
{
    // OBTENER TODOS LOS DATOS: GET api/configuracion/empresa
    [HttpGet("empresa")]
    public IActionResult GetEmpresa()
    {
        try
        {
            var datos = factory.ObtenerEmpresa();
            if (datos == null)
                return NotFound(new { message = "No se encontraron datos de configuración para la empresa." });

            return Ok(datos);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Error al obtener datos: " + ex.Message });
        }
    }

    // CREAR O ACTUALIZAR (UPSERT): POST api/configuracion/empresa
    [HttpPost("empresa")]
    public IActionResult SaveEmpresa(EmpresaDTO dto)
    {
        try
        {
            factory.GuardarEmpresa(dto);
            return Ok(new { success = true, message = "Datos de empresa procesados correctamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Error al guardar: " + ex.Message });
        }
    }

    // ACTUALIZAR REGISTRO EXISTENTE: PUT api/configuracion/empresa/1
    [HttpPut("empresa/{id}")]
    public IActionResult UpdateEmpresa(int id, EmpresaDTO dto)
    {
        try
        {
            // Forzamos el ID del DTO para que coincida con el de la URL (generalmente 1)
            dto.Id = id;
            factory.GuardarEmpresa(dto);
            return Ok(new { success = true, message = $"Datos de la empresa con ID {id} actualizados correctamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Error al actualizar: " + ex.Message });
        }
    }
}