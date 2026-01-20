using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ProductoController(IProductoService service) : ControllerBase
{
    [HttpGet] public IActionResult Get() => Ok(service.ObtenerTodos());
    [HttpPost] public IActionResult Post([FromBody] ProductoDTO dto) => Ok(service.Crear(dto));

    [HttpGet("alertas")]
    public IActionResult GetAlertas() => Ok(service.ObtenerAlertasStock());
}