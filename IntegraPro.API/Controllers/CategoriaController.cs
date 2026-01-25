using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriaController(ICategoriaService service) : ControllerBase
{
    private readonly ICategoriaService _service = service;

    [HttpGet]
    public IActionResult Get()
    {
        var response = _service.ObtenerTodas();
        // Usamos .Result (Mayúscula según tu clase ApiResponse)
        return response.Result ? Ok(response) : BadRequest(response);
    }

    [HttpPost]
    public IActionResult Post([FromBody] CategoriaDTO dto)
    {
        // Pasamos el ejecutor extraído del token para validar el rol 'solo_lectura'
        var response = _service.Crear(dto, ObtenerEjecutor());

        return response.Result ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Extrae la identidad del usuario desde el Token JWT para validación de permisos.
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

        // Usuario por defecto para desarrollo
        return new UsuarioDTO { Id = 1, RolId = 1, SucursalId = 1 };
    }
}