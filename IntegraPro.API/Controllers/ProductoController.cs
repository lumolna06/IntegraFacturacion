using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductoController(IProductoService service) : ControllerBase
{
    private readonly IProductoService _service = service;

    [HttpGet]
    public IActionResult Get()
    {
        var response = _service.ObtenerTodos(ObtenerEjecutor());
        return response.Result ? Ok(response) : BadRequest(response);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var response = _service.ObtenerPorId(id, ObtenerEjecutor());
        return response.Result ? Ok(response) : NotFound(response);
    }

    [HttpPost]
    public IActionResult Post([FromBody] ProductoDTO dto)
    {
        var response = _service.Crear(dto, ObtenerEjecutor());
        return response.Result ? Ok(response) : BadRequest(response);
    }

    [HttpPut]
    public IActionResult Put([FromBody] ProductoDTO dto)
    {
        var response = _service.Actualizar(dto, ObtenerEjecutor());
        return response.Result ? Ok(response) : BadRequest(response);
    }

    [HttpGet("alertas")]
    public IActionResult GetAlertas()
    {
        var response = _service.ObtenerAlertasStock(ObtenerEjecutor());
        return response.Result ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Extrae la identidad del usuario desde el Token JWT.
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

        // Usuario por defecto para desarrollo/pruebas
        return new UsuarioDTO { Id = 1, RolId = 1, SucursalId = 1 };
    }
}