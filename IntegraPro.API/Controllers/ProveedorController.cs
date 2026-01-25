using Microsoft.AspNetCore.Mvc;
using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProveedorController(IProveedorService service) : ControllerBase
{
    private readonly IProveedorService _service = service;

    // Obtener todos los proveedores (Ahora filtrados por permiso)
    [HttpGet]
    public IActionResult Get()
    {
        var response = _service.ObtenerTodos(ObtenerEjecutor());
        return response.Result ? Ok(response) : BadRequest(response);
    }

    // Crear un nuevo proveedor (Valida permisos de escritura)
    [HttpPost]
    public IActionResult Post([FromBody] ProveedorDTO proveedor)
    {
        var response = _service.Crear(proveedor, ObtenerEjecutor());
        // Usamos el mensaje dinámico del Service para el Ok
        return response.Result ? Ok(response) : StatusCode(403, response);
    }

    // Actualizar un proveedor existente
    [HttpPut]
    public IActionResult Put([FromBody] ProveedorDTO proveedor)
    {
        if (proveedor.Id <= 0)
            return BadRequest(new { mensaje = "ID de proveedor no válido para actualizar." });

        var response = _service.Actualizar(proveedor, ObtenerEjecutor());
        return response.Result ? Ok(response) : BadRequest(response);
    }

    // Eliminar (o desactivar) un proveedor
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        if (id <= 0)
            return BadRequest(new { mensaje = "ID de proveedor no válido." });

        var response = _service.Eliminar(id, ObtenerEjecutor());
        return response.Result ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Extrae la identidad del usuario para validar permisos en el Factory.
    /// </summary>
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
        // Mock de desarrollo: Rol 1 suele ser Admin con todos los permisos
        return new UsuarioDTO { Id = 1, RolId = 1, SucursalId = 1 };
    }
}