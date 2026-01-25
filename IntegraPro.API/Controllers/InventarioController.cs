using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventarioController(IInventarioService service, IUsuarioService usuarioService) : ControllerBase
{
    [HttpPost]
    public IActionResult Post([FromBody] MovimientoInventarioDTO dto)
    {
        // 1. Obtener el usuario ejecutor. 
        // Como tu UsuarioService no tiene ObtenerPorId, usamos GetByUsername o GetAll.
        // Lo ideal es que el ejecutor sea el mismo que está logueado.
        var usuariosResponse = usuarioService.ObtenerTodos(new UsuarioDTO { Id = dto.UsuarioId });

        // Desempaquetamos el ApiResponse de tu UsuarioService
        var ejecutor = usuariosResponse.Data?.FirstOrDefault(u => u.Id == dto.UsuarioId);

        if (ejecutor == null)
            return Unauthorized(new ApiResponse<bool>(false, "Usuario ejecutor no encontrado o sin permisos.", false));

        // 2. Llamar al servicio de inventario con el objeto ejecutor completo
        var result = service.Registrar(dto, ejecutor);

        return result.Result ? Ok(result) : BadRequest(result);
    }

    [HttpPost("producir")]
    public IActionResult Producir(int productoId, decimal cantidad, int usuarioId)
    {
        // 1. Buscamos al usuario ejecutor para validar sucursal y permisos de producción
        var usuariosResponse = usuarioService.ObtenerTodos(new UsuarioDTO { Id = usuarioId });
        var ejecutor = usuariosResponse.Data?.FirstOrDefault(u => u.Id == usuarioId);

        if (ejecutor == null)
            return Unauthorized(new ApiResponse<bool>(false, "Contexto de usuario inválido.", false));

        // 2. Ejecutar la lógica de explosión de materiales
        var result = service.ProcesarProduccion(productoId, cantidad, ejecutor);

        return result.Result ? Ok(result) : BadRequest(result);
    }
}