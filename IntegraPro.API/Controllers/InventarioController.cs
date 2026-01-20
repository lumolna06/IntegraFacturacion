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

    // NUEVO ENDPOINT: Para procesar la fabricación de productos elaborados
    [HttpPost("producir")]
    public IActionResult Producir(int productoId, decimal cantidad, int usuarioId)
    {
        // Este método descontará materiales y aumentará el producto terminado
        var result = service.ProcesarProduccion(productoId, cantidad, usuarioId);
        return result.Result ? Ok(result) : BadRequest(result);
    }
}