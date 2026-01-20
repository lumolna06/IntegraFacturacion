using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventarioController(IInventarioService service) : ControllerBase
{
    [HttpPost]
    public IActionResult Post([FromBody] MovimientoInventarioDTO dto)
    {
        var result = service.Registrar(dto);
        return result.Result ? Ok(result) : BadRequest(result);
    }
}