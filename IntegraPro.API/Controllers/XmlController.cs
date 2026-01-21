using Microsoft.AspNetCore.Mvc;
using IntegraPro.AppLogic.Services;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class XmlController(IConfiguration config) : ControllerBase
{
    private readonly string _conn = config.GetConnectionString("DefaultConnection")!;

    [HttpPost("preprocesar")]
    public IActionResult SubirXml(IFormFile archivo)
    {
        if (archivo == null) return BadRequest("No se recibió archivo.");

        using var reader = new StreamReader(archivo.OpenReadStream());
        string contenido = reader.ReadToEnd();

        var service = new XmlParserService(_conn);
        var resultado = service.ProcesarFacturaCR(contenido);

        return Ok(resultado);
    }
}