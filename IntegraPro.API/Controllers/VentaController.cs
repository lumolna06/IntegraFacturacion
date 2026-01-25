using IntegraPro.AppLogic.Services;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using IntegraPro.API.Reports;
using System.Data;
using System.Security.Claims;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VentaController(VentaService service) : ControllerBase
{
    private readonly VentaService _service = service;

    [HttpPost]
    public IActionResult Post([FromBody] FacturaDTO factura)
    {
        try
        {
            // Simulación de validación con Hacienda
            factura.EstadoHacienda = "ACEPTADO";
            factura.EsOffline = false;

            // Se pasa el ejecutor al método ProcesarVenta
            string numFac = _service.ProcesarVenta(factura, ObtenerEjecutor());

            return Ok(new
            {
                success = true,
                factura = numFac,
                estado = factura.EstadoHacienda,
                message = "Venta procesada y aceptada por Hacienda."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("reportes")]
    public IActionResult GetReportes(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? clienteId,
        [FromQuery] string buscar = "",
        [FromQuery] string? condicion = "")
    {
        try
        {
            // Aseguramos que las fechas no sean nulas para el Service
            DateTime fechaInicio = desde ?? DateTime.Now.AddMonths(-1);
            DateTime fechaFin = hasta ?? DateTime.Now;

            // LLAMADA CORREGIDA: Ahora coincide con los 6 parámetros del Service
            DataTable dt = _service.ObtenerReporteVentas(
                fechaInicio,
                fechaFin,
                clienteId,
                buscar ?? "",
                condicion ?? "",
                ObtenerEjecutor()
            );

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

                // Validamos que la columna exista antes de sumar
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
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("imprimir/{id}")]
    public IActionResult Imprimir(int id)
    {
        try
        {
            // Se pasa el ejecutor para validar si tiene permiso de ver esta factura
            var factura = _service.ObtenerFacturaParaImpresion(id, ObtenerEjecutor());

            if (factura == null)
                return NotFound(new { success = false, message = "La factura no existe o no tiene permisos para verla." });

            var empresa = _service.ObtenerEmpresa();

            QuestPDF.Settings.License = LicenseType.Community;

            var documento = new FacturaReport(factura, empresa);
            byte[] pdfBytes = documento.GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Factura_{factura.Consecutivo}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "Error al generar PDF: " + ex.Message });
        }
    }

    /// <summary>
    /// Obtiene el usuario autenticado desde el Token JWT.
    /// </summary>
    private UsuarioDTO ObtenerEjecutor()
    {
        // Se recomienda usar nombres de claims estándar o los definidos en tu Login
        if (User.Identity?.IsAuthenticated == true)
        {
            return new UsuarioDTO
            {
                Id = int.Parse(User.FindFirst("id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"),
                RolId = int.Parse(User.FindFirst("rolId")?.Value ?? "0"),
                SucursalId = int.Parse(User.FindFirst("sucursalId")?.Value ?? "0"),
                Username = User.Identity.Name ?? ""
            };
        }

        // Usuario por defecto solo para desarrollo
        return new UsuarioDTO { Id = 1, RolId = 1, SucursalId = 1, Username = "DevUser" };
    }
}