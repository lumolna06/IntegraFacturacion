using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConfiguracionController(IConfiguracionService service) : BaseController
{
    private readonly IConfiguracionService _service = service;

    /// <summary>
    /// MÉTODO MAESTRO: Registro inicial de Empresa + Usuario Admin.
    /// Es el que debe llamar tu JavaScript del Wizard.
    /// </summary>
    [HttpPost("finalizar-instalacion")]
    [AllowAnonymous] // Permitido porque el sistema aún no tiene usuarios
    public IActionResult FinalizarInstalacion([FromBody] RegistroInicialDTO modelo)
    {
        var res = _service.FinalizarInstalacion(modelo);
        return res.Result ? Ok(res) : BadRequest(res);
    }

    /// <summary>
    /// Obtiene los datos de la empresa. Requiere estar logueado.
    /// </summary>
    [HttpGet("empresa")]
    public IActionResult GetEmpresa()
    {
        var res = _service.ObtenerDatosEmpresa(UsuarioActual);
        return res.Result ? Ok(res) : NotFound(res);
    }

    /// <summary>
    /// Actualiza los datos de la empresa. Requiere Token.
    /// </summary>
    [HttpPut("empresa/{id}")]
    public IActionResult ActualizarEmpresa(int id, [FromBody] EmpresaDTO dto)
    {
        dto.Id = id;
        var res = _service.ActualizarEmpresa(dto, UsuarioActual);
        return res.Result ? Ok(res) : BadRequest(res);
    }

    // Nota: He removido el [HttpPost("empresa")] simple porque 
    // ahora usamos "finalizar-instalacion" para el Wizard.
}