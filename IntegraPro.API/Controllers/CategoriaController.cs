using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Asegura que las categorías solo sean accesibles con Token
public class CategoriaController(ICategoriaService service) : BaseController // Herencia aplicada
{
    private readonly ICategoriaService _service = service;

    [HttpGet]
    public IActionResult Get()
    {
        // Usamos la propiedad 'UsuarioActual' heredada de BaseController
        var response = _service.ObtenerTodas(UsuarioActual);
        return response.Result ? Ok(response) : BadRequest(response);
    }

    [HttpPost]
    public IActionResult Post([FromBody] CategoriaDTO dto)
    {
        var response = _service.Crear(dto, UsuarioActual);
        return response.Result ? Ok(response) : BadRequest(response);
    }
}