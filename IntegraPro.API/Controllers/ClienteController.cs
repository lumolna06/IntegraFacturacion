using Microsoft.AspNetCore.Mvc;
using IntegraPro.AppLogic.Interfaces; // IMPORTANTE: Usar la interfaz
using IntegraPro.DTO.Models;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
// Cambiamos la inyección a la Interfaz IClienteService
public class ClienteController(IClienteService service) : ControllerBase
{
    private readonly IClienteService _service = service;

    [HttpGet]
    public IActionResult Get()
    {
        // Pasamos ObtenerEjecutor() para cumplir con la nueva firma del Service
        var response = _service.ObtenerTodos(ObtenerEjecutor());
        return response.Result ? Ok(response) : BadRequest(response);
    }

    [HttpPost]
    public IActionResult Post([FromBody] ClienteDTO cliente)
    {
        // Cambiamos .Guardar por .Crear (nombre en la Interfaz)
        var response = _service.Crear(cliente, ObtenerEjecutor());

        if (response.Result)
            return Ok(new { success = true, idCreated = response.Data, message = response.Message });

        return BadRequest(new { success = false, message = response.Message });
    }

    [HttpPut]
    public IActionResult Put([FromBody] ClienteDTO cliente)
    {
        var response = _service.Actualizar(cliente, ObtenerEjecutor());

        if (response.Result)
            return Ok(new { success = true, message = response.Message });

        return BadRequest(new { success = false, message = response.Message });
    }

    [HttpGet("buscar/{identificacion}")]
    public IActionResult GetByIdentificacion(string identificacion)
    {
        var response = _service.BuscarPorIdentificacion(identificacion);

        if (!response.Result)
            return NotFound(new { success = false, message = response.Message });

        return Ok(new { success = true, data = response.Data });
    }

    private UsuarioDTO ObtenerEjecutor()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return new UsuarioDTO
            {
                Id = int.Parse(User.FindFirst("id")?.Value ?? "0"),
                RolId = int.Parse(User.FindFirst("rolId")?.Value ?? "0"),
                SucursalId = int.Parse(User.FindFirst("sucursalId")?.Value ?? "0")
            };
        }

        return new UsuarioDTO { Id = 1, RolId = 1, SucursalId = 1 };
    }
}