using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Exigimos Token JWT para cualquier operación de productos
public class ProductoController(IProductoService service) : BaseController // Herencia de tu BaseController real
{
    private readonly IProductoService _service = service;

    [HttpGet]
    public IActionResult Get()
    {
        // Usamos UsuarioActual (la propiedad real del BaseController)
        var response = _service.ObtenerTodos(UsuarioActual);
        return response.Result ? Ok(response) : BadRequest(response);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var response = _service.ObtenerPorId(id, UsuarioActual);
        return response.Result ? Ok(response) : NotFound(response);
    }

    [HttpPost]
    public IActionResult Post([FromBody] ProductoDTO dto)
    {
        var response = _service.Crear(dto, UsuarioActual);
        return response.Result ? Ok(response) : BadRequest(response);
    }

    [HttpPut]
    public IActionResult Put([FromBody] ProductoDTO dto)
    {
        var response = _service.Actualizar(dto, UsuarioActual);
        return response.Result ? Ok(response) : BadRequest(response);
    }

    [HttpGet("alertas")]
    public IActionResult GetAlertas()
    {
        var response = _service.ObtenerAlertasStock(UsuarioActual);
        return response.Result ? Ok(response) : BadRequest(response);
    }
}