using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IntegraPro.DTO.Models;

namespace IntegraPro.API.Reports;

public class FacturaReport(FacturaDTO model, EmpresaDTO empresa) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        string regimen = empresa.TipoRegimen?.ToUpper() ?? "TRADICIONAL";

        container.Page(page =>
        {
            page.Margin(1f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontFamily(Fonts.Arial).FontSize(10));

            // ENCABEZADO
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(empresa.NombreComercial.ToUpper()).FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                    col.Item().Text(empresa.RazonSocial).FontSize(10).SemiBold();
                    col.Item().Text($"Cédula Jurídica: {empresa.CedulaJuridica}");

                    if (!string.IsNullOrEmpty(empresa.Telefono))
                        col.Item().Text($"Teléfono: {empresa.Telefono}");

                    if (regimen == "SIMPLIFICADO")
                        col.Item().PaddingTop(5f).Text("Régimen Simplificado").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("FACTURA ELECTRÓNICA").FontSize(16).Light();
                    col.Item().Text($"# {model.Consecutivo ?? "000000"}").Bold();
                    col.Item().Text($"Fecha: {model.Fecha:dd/MM/yyyy HH:mm}");
                    col.Item().Text($"Condición: {model.CondicionVenta}").FontSize(9);
                });
            });

            // CONTENIDO
            page.Content().PaddingVertical(0.5f, Unit.Centimetre).Column(col =>
            {
                // SECCIÓN CLIENTE (IGUAL A PROFORMA)
                col.Item().PaddingBottom(5f).BorderBottom(1f).BorderColor(Colors.Grey.Lighten2).Row(row =>
                {
                    row.RelativeItem().Column(c => {
                        c.Item().Text(t => {
                            t.Span("CLIENTE: ").Bold();
                            t.Span(model.ClienteNombre?.ToUpper() ?? "CLIENTE CONTADO");
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
                        t.Span("PAGO: ").Bold();
                        t.Span(model.MedioPago.ToUpper());
                    });
                });

                // TABLA DE PRODUCTOS
                col.Item().PaddingTop(10f).Table(table =>
                {
                    table.ColumnsDefinition(columns => {
                        columns.RelativeColumn(3f);
                        columns.RelativeColumn(0.7f);
                        columns.RelativeColumn(1.2f);
                        if (regimen != "SIMPLIFICADO")
                        {
                            columns.RelativeColumn(0.6f);
                            columns.RelativeColumn(1f);
                        }
                        columns.RelativeColumn(1.2f);
                    });

                    table.Header(header => {
                        header.Cell().BorderBottom(1f).PaddingBottom(5f).Text("Producto").Bold();
                        header.Cell().BorderBottom(1f).PaddingBottom(5f).AlignRight().Text("Cant.").Bold();
                        header.Cell().BorderBottom(1f).PaddingBottom(5f).AlignRight().Text("P. Unit").Bold();
                        if (regimen != "SIMPLIFICADO")
                        {
                            header.Cell().BorderBottom(1f).PaddingBottom(5f).AlignRight().Text("%").Bold();
                            header.Cell().BorderBottom(1f).PaddingBottom(5f).AlignRight().Text("IVA").Bold();
                        }
                        header.Cell().BorderBottom(1f).PaddingBottom(5f).AlignRight().Text("Total").Bold();
                    });

                    foreach (var item in model.Detalles)
                    {
                        table.Cell().PaddingVertical(2f).BorderBottom(1f).BorderColor(Colors.Grey.Lighten4).Text(item.ProductoNombre ?? "N/A").FontSize(9);
                        table.Cell().PaddingVertical(2f).BorderBottom(1f).BorderColor(Colors.Grey.Lighten4).AlignRight().Text(item.Cantidad.ToString("N2"));
                        table.Cell().PaddingVertical(2f).BorderBottom(1f).BorderColor(Colors.Grey.Lighten4).AlignRight().Text(item.PrecioUnitario.ToString("N2"));

                        if (regimen != "SIMPLIFICADO")
                        {
                            table.Cell().PaddingVertical(2f).BorderBottom(1f).BorderColor(Colors.Grey.Lighten4).AlignRight().Text($"{item.PorcentajeImpuesto:N0}%").FontSize(8);
                            table.Cell().PaddingVertical(2f).BorderBottom(1f).BorderColor(Colors.Grey.Lighten4).AlignRight().Text(item.MontoImpuesto.ToString("N2"));
                        }
                        table.Cell().PaddingVertical(2f).BorderBottom(1f).BorderColor(Colors.Grey.Lighten4).AlignRight().Text(item.TotalLinea.ToString("N2")).Bold();
                    }
                });

                // TOTALES
                col.Item().AlignRight().PaddingTop(10f).MinWidth(200f).Column(c =>
                {
                    c.Item().Row(r => {
                        r.RelativeItem().Text("Subtotal:").AlignRight();
                        r.ConstantItem(85f).AlignRight().Text($"{model.TotalNeto:N2}");
                    });

                    if (regimen != "SIMPLIFICADO")
                    {
                        c.Item().Row(r => {
                            r.RelativeItem().Text("IVA:").AlignRight();
                            r.ConstantItem(85f).AlignRight().Text($"{model.TotalImpuesto:N2}");
                        });
                    }

                    c.Item().PaddingTop(5f).BorderTop(1f).Row(r => {
                        r.RelativeItem().Text("TOTAL:").FontSize(14).Bold().FontColor(Colors.Blue.Medium).AlignRight();
                        r.ConstantItem(85f).AlignRight().Text($"{model.TotalComprobante:N2}").FontSize(14).Bold().FontColor(Colors.Blue.Medium);
                    });
                });

                if (!string.IsNullOrEmpty(model.ClaveNumerica))
                {
                    col.Item().PaddingTop(15f).Text(t => {
                        t.Span("Clave: ").Bold().FontSize(8);
                        t.Span(model.ClaveNumerica).FontSize(8);
                    });
                }
            });

            page.Footer().AlignCenter().Column(c => {
                c.Item().Text("Autorizada mediante resolución DGT-R-033-2019").FontSize(7).FontColor(Colors.Grey.Medium);
                c.Item().PaddingTop(2f).Text(x => {
                    x.Span("Página ").FontSize(8);
                    x.CurrentPageNumber().FontSize(8);
                    x.Span(" de ").FontSize(8);
                    x.TotalPages().FontSize(8);
                });
            });
        });
    }
}