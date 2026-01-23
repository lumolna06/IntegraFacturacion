using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IntegraPro.DTO.Models;

namespace IntegraPro.API.Reports;

public class ProformaReport(ProformaEncabezadoDTO model, EmpresaDTO empresa) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(1, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontFamily(Fonts.Arial).FontSize(10));

            // ==========================================
            // ENCABEZADO: DATOS EMPRESA Y DOCUMENTO
            // ==========================================
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(empresa.NombreComercial.ToUpper()).FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                    col.Item().Text($"Cédula Jurídica: {empresa.CedulaJuridica}");

                    if (!string.IsNullOrEmpty(empresa.Telefono))
                        col.Item().Text($"Teléfono: {empresa.Telefono}");

                    if (!string.IsNullOrEmpty(empresa.CorreoNotificaciones))
                    {
                        col.Item().Row(r =>
                        {
                            r.AutoItem().Text("Email: ");
                            r.RelativeItem().Hyperlink($"mailto:{empresa.CorreoNotificaciones}")
                                .Text(empresa.CorreoNotificaciones.ToLower())
                                .FontColor(Colors.Blue.Medium)
                                .Underline();
                        });
                    }
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("PROFORMA").FontSize(22).Light();
                    col.Item().Text($"# {model.Id:D6}").Bold();
                    col.Item().Text($"Fecha: {model.Fecha:dd/MM/yyyy}");
                    col.Item().Text($"Vence: {model.FechaVencimiento:dd/MM/yyyy}").FontSize(9).Italic();
                });
            });

            // ==========================================
            // CONTENIDO: CLIENTE Y TABLA DE PRODUCTOS
            // ==========================================
            page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
            {
                // SECCIÓN CLIENTE
                col.Item().PaddingBottom(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Row(row =>
                {
                    row.RelativeItem().Column(c => {
                        c.Item().Text(t => {
                            t.Span("CLIENTE: ").Bold();
                            t.Span(model.ClienteNombre?.ToUpper() ?? "N/A");
                        });

                        if (!string.IsNullOrEmpty(model.ClienteIdentificacion))
                        {
                            c.Item().Text(t => {
                                t.Span("CÉDULA: ").Bold().FontSize(9);
                                t.Span(model.ClienteIdentificacion).FontSize(9);
                            });
                        }
                    });

                    row.RelativeItem().AlignRight().Text(t => {
                        t.Span("ESTADO: ").Bold();
                        t.Span(model.Estado.ToUpper()).FontColor(model.Estado == "Pendiente" ? Colors.Orange.Medium : Colors.Green.Medium);
                    });
                });

                // TABLA DE PRODUCTOS (CON DESGLOSE DE IVA)
                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns => {
                        columns.RelativeColumn(1f);    // Código
                        columns.RelativeColumn(2.5f);  // Producto
                        columns.RelativeColumn(0.6f);  // Cant.
                        columns.RelativeColumn(1.1f);  // P. Neto
                        columns.RelativeColumn(0.5f);  // %
                        columns.RelativeColumn(1f);    // IVA
                        columns.RelativeColumn(1.2f);  // Total
                    });

                    table.Header(header => {
                        header.Cell().BorderBottom(1).PaddingBottom(5).Text("Código").Bold();
                        header.Cell().BorderBottom(1).PaddingBottom(5).Text("Producto").Bold();
                        header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Cant.").Bold();
                        header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("P. Neto").Bold();
                        header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("%").Bold();
                        header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("IVA").Bold();
                        header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Total").Bold();
                    });

                    foreach (var item in model.Detalles)
                    {
                        table.Cell().PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Text(item.ProductoCodigo).FontSize(8);
                        table.Cell().PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Text(item.ProductoNombre).FontSize(9);
                        table.Cell().PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).AlignRight().Text(item.Cantidad.ToString("N2"));
                        table.Cell().PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).AlignRight().Text(item.PrecioUnitario.ToString("N2"));
                        table.Cell().PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).AlignRight().Text($"{item.PorcentajeImpuesto:N0}%").FontSize(8);
                        table.Cell().PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).AlignRight().Text(item.ImpuestoTotal.ToString("N2"));
                        table.Cell().PaddingVertical(2).BorderBottom(1).BorderColor(Colors.Grey.Lighten4).AlignRight().Text(item.TotalLineas.ToString("N2")).Bold();
                    }
                });

                // ==========================================
                // SECCIÓN DE TOTALES FINALES
                // ==========================================
                col.Item().AlignRight().PaddingTop(10).MinWidth(180).Column(c =>
                {
                    c.Item().Row(r => {
                        r.RelativeItem().Text("Subtotal Neto:").AlignRight();
                        r.ConstantItem(85).AlignRight().Text($"{model.TotalNeto:N2}");
                    });

                    c.Item().Row(r => {
                        r.RelativeItem().Text("Total Impuestos:").AlignRight();
                        r.ConstantItem(85).AlignRight().Text($"{model.TotalImpuesto:N2}");
                    });

                    c.Item().PaddingTop(5).BorderTop(1).Row(r => {
                        r.RelativeItem().Text("TOTAL COMPROBANTE:").FontSize(12).Bold().FontColor(Colors.Blue.Medium).AlignRight();
                        r.ConstantItem(85).AlignRight().Text($"{model.Total:N2}").FontSize(12).Bold().FontColor(Colors.Blue.Medium);
                    });
                });
            });

            // PIE DE PÁGINA
            page.Footer().AlignCenter().Column(c => {
                c.Item().Text(x =>
                {
                    x.Span("Gracias por su preferencia. ").FontSize(9);
                    x.Span($"Esta proforma es válida hasta el {model.FechaVencimiento:dd/MM/yyyy}.").FontSize(9).Italic();
                });

                c.Item().PaddingTop(5).Text(x => {
                    x.Span("Página ").FontSize(8);
                    x.CurrentPageNumber().FontSize(8);
                    x.Span(" de ").FontSize(8);
                    x.TotalPages().FontSize(8);
                });
            });
        });
    }
}