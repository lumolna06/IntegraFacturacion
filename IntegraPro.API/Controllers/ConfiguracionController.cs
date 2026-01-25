using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Bloqueo global: nadie ve la config sin Token
public class ConfiguracionController(IConfiguracionService service) : BaseController // Hereda de BaseController
{
    private readonly IConfiguracionService _service = service;

    [HttpGet("empresa")]
    public IActionResult GetEmpresa()
    {
        // Usamos UsuarioActual de la clase base (elimina el Mock de Id=1)
        var res = _service.ObtenerDatosEmpresa(UsuarioActual);
        return res.Result ? Ok(res) : NotFound(res);
    }

    [HttpPost("empresa")]
    public IActionResult SaveEmpresa([FromBody] EmpresaDTO dto)
    {
        var res = _service.ActualizarEmpresa(dto, UsuarioActual);
        return res.Result ? Ok(res) : BadRequest(res);
    }

    [HttpPut("empresa/{id}")]
    public IActionResult UpdateEmpresa(int id, [FromBody] EmpresaDTO dto)
    {
        dto.Id = id;
        var res = _service.ActualizarEmpresa(dto, UsuarioActual);
        return res.Result ? Ok(res) : BadRequest(res);
    }
}