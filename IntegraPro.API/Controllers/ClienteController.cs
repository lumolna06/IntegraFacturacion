using Microsoft.AspNetCore.Mvc;
using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Authorization;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Protege todos los endpoints del controlador
public class ClienteController(IClienteService service) : BaseController // Hereda de BaseController
{
    private readonly IClienteService _service = service;

    [HttpGet]
    public IActionResult Get()
    {
        // Usamos UsuarioActual de la clase base
        var response = _service.ObtenerTodos(UsuarioActual);
        return response.Result ? Ok(response) : BadRequest(response);
    }

    [HttpPost]
    public IActionResult Post([FromBody] ClienteDTO cliente)
    {
        var response = _service.Crear(cliente, UsuarioActual);

        if (response.Result)
            return Ok(new { success = true, idCreated = response.Data, message = response.Message });

        return BadRequest(new { success = false, message = response.Message });
    }

    [HttpPut]
    public IActionResult Put([FromBody] ClienteDTO cliente)
    {
        var response = _service.Actualizar(cliente, UsuarioActual);

        if (response.Result)
            return Ok(new { success = true, message = response.Message });

        return BadRequest(new { success = false, message = response.Message });
    }

    [HttpGet("buscar/{identificacion}")]
    public IActionResult GetByIdentificacion(string identificacion)
    {
        // IMPORTANTE: Se añade UsuarioActual para que el Service valide permisos de lectura
        var response = _service.BuscarPorIdentificacion(identificacion, UsuarioActual);

        if (!response.Result)
            return NotFound(new { success = false, message = response.Message });

        return Ok(new { success = true, data = response.Data });
    }
}