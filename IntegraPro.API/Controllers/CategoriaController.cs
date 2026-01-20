using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CategoriaController(ICategoriaService service) : ControllerBase
{
    [HttpGet] public IActionResult Get() => Ok(service.ObtenerTodas());
    [HttpPost] public IActionResult Post([FromBody] CategoriaDTO dto) => Ok(service.Crear(dto));
}