using Microsoft.AspNetCore.Mvc;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProveedorController(ProveedorFactory proveedorFactory) : ControllerBase
{
    private readonly ProveedorFactory _factory = proveedorFactory;

    // Obtener todos los proveedores
    [HttpGet]
    public IActionResult Get() => Ok(_factory.ObtenerTodos());

    // Crear un nuevo proveedor
    [HttpPost]
    public IActionResult Post(ProveedorDTO proveedor)
    {
        _factory.Crear(proveedor);
        return Ok(new { mensaje = "Proveedor guardado correctamente" });
    }

    // Actualizar un proveedor existente
    [HttpPut]
    public IActionResult Put(ProveedorDTO proveedor)
    {
        if (proveedor.Id <= 0) return BadRequest("ID de proveedor no válido para actualizar.");
        _factory.Actualizar(proveedor);
        return Ok(new { mensaje = "Proveedor actualizado correctamente" });
    }

    // Eliminar (o desactivar) un proveedor
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        if (id <= 0) return BadRequest("ID de proveedor no válido.");
        _factory.Eliminar(id);
        return Ok(new { mensaje = "Proveedor eliminado correctamente" });
    }
}