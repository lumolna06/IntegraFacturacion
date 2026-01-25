using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Protegemos el acceso global al controlador
public class ProveedorController(IProveedorService service) : BaseController // Herencia de BaseController
{
    private readonly IProveedorService _service = service;

    // Obtener todos los proveedores
    [HttpGet]
    public IActionResult Get()
    {
        // UsuarioActual viene del BaseController con todos sus Claims
        var response = _service.ObtenerTodos(UsuarioActual);
        return response.Result ? Ok(response) : BadRequest(response);
    }

    // Crear un nuevo proveedor
    [HttpPost]
    public IActionResult Post([FromBody] ProveedorDTO proveedor)
    {
        var response = _service.Crear(proveedor, UsuarioActual);

        // Si el servicio lanza un UnauthorizedAccessException, el BaseController o 
        // la lógica del Service lo atrapará. Aquí retornamos 403 si falla el permiso.
        if (!response.Result && response.Message.Contains("permiso"))
            return StatusCode(403, response);

        return response.Result ? Ok(response) : BadRequest(response);
    }

    // Actualizar un proveedor existente
    [HttpPut]
    public IActionResult Put([FromBody] ProveedorDTO proveedor)
    {
        if (proveedor.Id <= 0)
            return BadRequest(new ApiResponse<bool>(false, "ID de proveedor no válido para actualizar."));

        var response = _service.Actualizar(proveedor, UsuarioActual);
        return response.Result ? Ok(response) : BadRequest(response);
    }

    // Eliminar (o desactivar) un proveedor
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        if (id <= 0)
            return BadRequest(new ApiResponse<bool>(false, "ID de proveedor no válido."));

        var response = _service.Eliminar(id, UsuarioActual);
        return response.Result ? Ok(response) : BadRequest(response);
    }
}