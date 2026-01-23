using IntegraPro.AppLogic.Services;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using IntegraPro.API.Reports;
using System.Data;

namespace IntegraPro.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VentaController(VentaService service) : ControllerBase
{
    private readonly VentaService _service = service;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] FacturaDTO factura)
    {
        try
        {
            try
            {
                factura.EstadoHacienda = "ACEPTADO";
                factura.EsOffline = false;
            }
            catch (Exception)
            {
                factura.EstadoHacienda = "PENDIENTE";
                factura.EsOffline = true;
            }

            string numFac = _service.ProcesarVenta(factura);

            return Ok(new
            {
                success = true,
                factura = numFac,
                estado = factura.EstadoHacienda,
                offline = factura.EsOffline,
                message = factura.EsOffline
                    ? "Venta guardada LOCALMENTE (Hacienda fuera de línea). Se enviará luego."
                    : "Venta procesada y aceptada por Hacienda."
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
        [FromQuery] int? sucursalId,
        [FromQuery] string buscar = "",
        [FromQuery] string? condicion = "") // NUEVO PARÁMETRO
    {
        try
        {
            // Pasamos 'condicion' al service (que ya lo espera tras la modificación anterior)
            DataTable dt = _service.ObtenerReporteVentas(desde, hasta, clienteId, sucursalId, buscar, condicion);

            decimal sumaTotales = 0;

            // Convertimos el DataTable a una lista de diccionarios para evitar el error de serialización
            var filas = new List<Dictionary<string, object>>();

            foreach (DataRow row in dt.Rows)
            {
                var diccionario = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    diccionario[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                }

                filas.Add(diccionario);

                // Sumatoria para el resumen
                if (row["total_comprobante"] != DBNull.Value)
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
            var factura = _service.ObtenerFacturaParaImpresion(id);
            if (factura == null)
                return NotFound(new { success = false, message = "La factura no existe." });

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
}