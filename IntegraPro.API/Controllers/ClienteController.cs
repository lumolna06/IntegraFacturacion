using Microsoft.AspNetCore.Mvc;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClienteController(IConfiguration config) : ControllerBase
{
    // Usamos la cadena de conexión configurada en tu appsettings.json
    private readonly ClienteFactory _factory = new(config.GetConnectionString("DefaultConnection")!);

    [HttpGet]
    public IActionResult Get()
    {
        try
        {
            return Ok(_factory.ObtenerTodos());
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public IActionResult Post([FromBody] ClienteDTO cliente)
    {
        try
        {
            int id = _factory.Insertar(cliente);
            return Ok(new { success = true, idCreated = id, message = "Cliente guardado." });
        }
        catch (Exception ex)
        {
            // Útil para detectar si la identificación ya existe (Unique Constraint)
            return BadRequest(new { success = false, message = "Error: " + ex.Message });
        }
    }

    [HttpPut]
    public IActionResult Put([FromBody] ClienteDTO cliente)
    {
        try
        {
            _factory.Actualizar(cliente);
            return Ok(new { success = true, message = "Cliente actualizado." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}