using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Bloqueo de seguridad: Requiere Token JWT
public class InventarioController(IInventarioService service) : BaseController // Herencia aplicada
{
    private readonly IInventarioService _service = service;

    /// <summary>
    /// Registra un movimiento de inventario manual.
    /// POST: api/inventario
    /// </summary>
    [HttpPost]
    public IActionResult Post([FromBody] MovimientoInventarioDTO dto)
    {
        // Usamos 'UsuarioActual' del BaseController. 
        // Si el token es inválido, el BaseController ya maneja el fallback o [Authorize] rebota la petición.
        var result = _service.Registrar(dto, UsuarioActual);

        return result.Result ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Procesa la producción (Explosión de materiales).
    /// POST: api/inventario/producir
    /// </summary>
    [HttpPost("producir")]
    public IActionResult Producir([FromQuery] int productoId, [FromQuery] decimal cantidad)
    {
        // Al usar 'UsuarioActual', garantizamos que se use la SucursalId real del usuario logueado
        var result = _service.ProcesarProduccion(productoId, cantidad, UsuarioActual);

        return result.Result ? Ok(result) : BadRequest(result);
    }
}