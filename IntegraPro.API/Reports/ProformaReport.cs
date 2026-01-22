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

            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(empresa.NombreComercial.ToUpper()).FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                    col.Item().Text($"Cédula: {empresa.CedulaJuridica}");
                    col.Item().Text($"Email: {empresa.CorreoNotificaciones}");
                    col.Item().Text($"Régimen: {empresa.TipoRegimen}");
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("PROFORMA").FontSize(22).Light();
                    col.Item().Text($"# {model.Id:D6}").Bold();
                    col.Item().Text($"Fecha: {model.Fecha:dd/MM/yyyy}");
                });
            });

            page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
            {
                col.Item().PaddingBottom(10).Text(t => {
                    t.Span("CLIENTE: ").Bold();
                    t.Span(model.ClienteNombre);
                });

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns => {
                        columns.RelativeColumn(3); columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn();
                    });

                    table.Header(header => {
                        header.Cell().BorderBottom(1).Text("Producto").Bold();
                        header.Cell().BorderBottom(1).AlignRight().Text("Cant.").Bold();
                        header.Cell().BorderBottom(1).AlignRight().Text("Precio").Bold();
                        header.Cell().BorderBottom(1).AlignRight().Text("Total").Bold();
                    });

                    foreach (var item in model.Detalles)
                    {
                        table.Cell().PaddingVertical(2).Text(item.ProductoNombre);
                        table.Cell().PaddingVertical(2).AlignRight().Text(item.Cantidad.ToString());
                        table.Cell().PaddingVertical(2).AlignRight().Text(item.PrecioUnitario.ToString("N2"));
                        table.Cell().PaddingVertical(2).AlignRight().Text(item.TotalLineas.ToString("N2"));
                    }
                });

                col.Item().AlignRight().PaddingTop(20)
                   .Text($"TOTAL: {model.Total:N2}").FontSize(14).Bold().FontColor(Colors.Blue.Medium);
            });
        });
    }
}