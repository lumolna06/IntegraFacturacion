using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracionController(IConfiguracionService service) : ControllerBase
{
    private readonly IConfiguracionService _service = service;

    // GET api/configuracion/empresa
    [HttpGet("empresa")]
    public IActionResult GetEmpresa()
    {
        var res = _service.ObtenerDatosEmpresa();
        return res.Result ? Ok(res) : NotFound(res);
    }

    // POST api/configuracion/empresa
    [HttpPost("empresa")]
    public IActionResult SaveEmpresa([FromBody] EmpresaDTO dto)
    {
        // ERROR CS7036 EVITADO: Enviamos el ejecutor para validar permisos 'config'
        var res = _service.ActualizarEmpresa(dto, ObtenerEjecutor());
        return res.Result ? Ok(res) : BadRequest(res);
    }

    // PUT api/configuracion/empresa/1
    [HttpPut("empresa/{id}")]
    public IActionResult UpdateEmpresa(int id, [FromBody] EmpresaDTO dto)
    {
        dto.Id = id;
        var res = _service.ActualizarEmpresa(dto, ObtenerEjecutor());
        return res.Result ? Ok(res) : BadRequest(res);
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
                SucursalId = int.Parse(User.FindFirst("sucursalId")?.Value ?? "0"),
                // Importante: El diccionario de permisos debe cargarse aquí si lo usas
                Permisos = new Dictionary<string, bool> { { "config", User.IsInRole("Admin") } }
            };
        }

        // Usuario temporal para desarrollo si no hay token (quitar en producción)
        return new UsuarioDTO { Id = 1, RolId = 1, SucursalId = 1, Permisos = new Dictionary<string, bool> { { "config", true } } };
    }
}