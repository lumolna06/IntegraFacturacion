using IntegraPro.AppLogic.Services;
using IntegraPro.DTO.Models;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using IntegraPro.API.Reports;

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
            // 1. Lógica de comunicación con el Proveedor de Facturación (Simulada)
            // En un escenario real, aquí llamarías a una clase 'FacturacionProvider'
            try
            {
                // Supongamos que llamas al API de tu proveedor aquí
                // var respuesta = await _proveedorService.EnviarHacienda(factura);

                // Si la comunicación es exitosa:
                factura.EstadoHacienda = "ACEPTADO"; // O el estado que devuelva Hacienda
                factura.EsOffline = false;
                // factura.ClaveNumerica = respuesta.Clave; // La clave que generó el proveedor
            }
            catch (Exception)
            {
                // Si falla el internet o el proveedor está caído:
                factura.EstadoHacienda = "PENDIENTE";
                factura.EsOffline = true;
                // No lanzamos excepción aquí para permitir que la venta se guarde localmente
            }

            // 2. El servicio procesa la venta en la DB con los estados actualizados
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