using IntegraPro.AppLogic.Interfaces; // Usar la interfaz para Inyección de Dependencias
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using IntegraPro.API.Reports;
using System.Data;
using System.Security.Claims;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Asegura que nadie sin Token JWT pueda facturar o ver reportes
public class VentaController(IVentaService service) : ControllerBase
{
    private readonly IVentaService _service = service;

    [HttpPost]
    public IActionResult Post([FromBody] FacturaDTO factura)
    {
        // Forzamos datos de Hacienda (Simulación)
        factura.EstadoHacienda = "ACEPTADO";
        factura.EsOffline = false;

        var response = _service.ProcesarVenta(factura, ObtenerEjecutor());

        if (!response.Result)
            return BadRequest(response);

        return Ok(new
        {
            success = true,
            factura = response.Data, // El consecutivo generado
            estado = factura.EstadoHacienda,
            message = response.Message
        });
    }

    [HttpGet("reportes")]
    public IActionResult GetReportes(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? clienteId,
        [FromQuery] string buscar = "",
        [FromQuery] string? condicion = "")
    {
        DateTime fechaInicio = desde ?? DateTime.Now.AddMonths(-1);
        DateTime fechaFin = hasta ?? DateTime.Now;

        var response = _service.ObtenerReporteVentas(
            fechaInicio,
            fechaFin,
            clienteId,
            buscar ?? "",
            condicion ?? "",
            ObtenerEjecutor()
        );

        if (!response.Result)
            return BadRequest(response);

        DataTable dt = response.Data;
        decimal sumaTotales = 0;
        var filas = new List<Dictionary<string, object>>();

        foreach (DataRow row in dt.Rows)
        {
            var diccionario = new Dictionary<string, object>();
            foreach (DataColumn col in dt.Columns)
            {
                diccionario[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
            }
            filas.Add(diccionario);

            if (dt.Columns.Contains("total_comprobante") && row["total_comprobante"] != DBNull.Value)
                sumaTotales += Convert.ToDecimal(row["total_comprobante"]);
        }

        return Ok(new
        {
            success = true,
            conteo = filas.Count,
            totalSuma = sumaTotales,
            data = filas
        });
    }

    [HttpGet("imprimir/{id}")]
    public IActionResult Imprimir(int id)
    {
        var ejecutor = ObtenerEjecutor();

        // 1. Obtener la factura
        var responseFactura = _service.ObtenerFacturaParaImpresion(id, ejecutor);
        if (!responseFactura.Result || responseFactura.Data == null)
            return NotFound(responseFactura);

        // 2. Obtener datos de la empresa (Ahora requiere ejecutor)
        var responseEmpresa = _service.ObtenerEmpresa(ejecutor);
        if (!responseEmpresa.Result || responseEmpresa.Data == null)
            return BadRequest(responseEmpresa);

        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var documento = new FacturaReport(responseFactura.Data, responseEmpresa.Data);
            byte[] pdfBytes = documento.GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Factura_{responseFactura.Data.Consecutivo}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Error al renderizar PDF: " + ex.Message });
        }
    }

    /// <summary>
    /// Extrae de forma segura la identidad del usuario desde el Token JWS.
    /// </summary>
    private UsuarioDTO ObtenerEjecutor()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return new UsuarioDTO
            {
                Id = int.Parse(User.FindFirst("Id")?.Value ?? "0"),
                RolId = int.Parse(User.FindFirst("RolId")?.Value ?? "0"),
                SucursalId = int.Parse(User.FindFirst("SucursalId")?.Value ?? "0"),
                Username = User.Identity.Name ?? "System",
                // Cargamos permisos desde el token si existen
                PermisosJson = User.FindFirst("permisos")?.Value
            };
        }

        // En producción, esto debería lanzar un error de No Autorizado
        throw new UnauthorizedAccessException("Debe estar autenticado para realizar esta operación.");
    }
}