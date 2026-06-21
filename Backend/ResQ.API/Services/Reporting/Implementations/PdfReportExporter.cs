using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResQ.API.Models.Reporting;

namespace ResQ.API.Services.Reporting.Implementations;

/// <summary>
/// Renders a <see cref="ReportModel"/> to a branded PDF document using QuestPDF.
/// Produces an A4 document with a ResQ header, KPI summary cards, a formatted data table,
/// an optional totals row, and a footer with page numbers and the generation timestamp.
/// </summary>
public class PdfReportExporter : IReportExporter
{
    /// <inheritdoc />
    public ReportFormat Format => ReportFormat.Pdf;

    // ResQ brand palette
    private const string Evergreen = "#1B3A2F";
    private const string Fern      = "#3E6B4F";
    private const string Lime      = "#D4E8B8";
    private const string Hunter    = "#2E5339";

    /// <inheritdoc />
    public ReportFile Export(ReportModel model, string baseFileName)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                page.Header().Element(c => ComposeHeader(c, model));
                page.Content().Element(c => ComposeContent(c, model));
                page.Footer().Element(c => ComposeFooter(c, model));
            });
        }).GeneratePdf();

        return new ReportFile(bytes, "application/pdf", $"{baseFileName}.pdf");
    }

    // ── Header: ResQ wordmark + report title/subtitle ──
    private static void ComposeHeader(IContainer container, ReportModel model)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("ResQ").FontSize(20).Bold().FontColor(Evergreen);
                    c.Item().Text("Plataforma de rescate alimentario").FontSize(8).FontColor(Fern);
                });

                row.ConstantItem(160).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(model.Title).FontSize(12).Bold().FontColor(Hunter);
                    if (!string.IsNullOrWhiteSpace(model.Subtitle))
                        c.Item().AlignRight().Text(model.Subtitle).FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Evergreen);
        });
    }

    // ── Content: KPI cards + data table ──
    private static void ComposeContent(IContainer container, ReportModel model)
    {
        container.PaddingVertical(12).Column(col =>
        {
            col.Spacing(14);

            if (model.Kpis.Count > 0)
                col.Item().Element(c => ComposeKpis(c, model));

            if (model.Columns.Count > 0)
                col.Item().Element(c => ComposeTable(c, model));
        });
    }

    // ── KPI summary cards laid out in rows of three ──
    private static void ComposeKpis(IContainer container, ReportModel model)
    {
        container.Column(col =>
        {
            col.Spacing(6);
            foreach (var chunk in model.Kpis.Chunk(3))
            {
                col.Item().Row(row =>
                {
                    row.Spacing(6);
                    foreach (var kpi in chunk)
                    {
                        row.RelativeItem().Background(Lime).Padding(8).Column(c =>
                        {
                            c.Item().Text(kpi.Label).FontSize(7.5f).FontColor(Hunter);
                            c.Item().Text(kpi.Value).FontSize(13).Bold().FontColor(Evergreen);
                        });
                    }

                    // pad the last row so cards keep a uniform width
                    for (var i = chunk.Length; i < 3; i++)
                        row.RelativeItem();
                });
            }
        });
    }

    // ── Tabular data with header, rows, and optional totals ──
    private static void ComposeTable(IContainer container, ReportModel model)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var col in model.Columns)
                    columns.RelativeColumn(col.Width);
            });

            table.Header(header =>
            {
                foreach (var col in model.Columns)
                {
                    var cell = header.Cell().Background(Evergreen).Padding(5);
                    var text = cell.Text(col.Header).FontColor(Colors.White).Bold().FontSize(8.5f);
                    if (col.Type is not ReportColumnType.Text) text.AlignRight();
                }
            });

            var striped = false;
            foreach (var dataRow in model.Rows)
            {
                var background = striped ? Colors.Grey.Lighten4 : Colors.White;
                striped = !striped;

                for (var c = 0; c < model.Columns.Count; c++)
                {
                    var raw  = c < dataRow.Length ? dataRow[c] : null;
                    var col  = model.Columns[c];
                    var cell = table.Cell().Background(background).PaddingVertical(4).PaddingHorizontal(5);
                    var text = cell.Text(ReportCellFormatter.Format(raw, col.Type)).FontSize(8.5f);
                    if (col.Type is not ReportColumnType.Text) text.AlignRight();
                }
            }

            if (model.TotalsRow is not null)
            {
                for (var c = 0; c < model.Columns.Count; c++)
                {
                    var raw  = c < model.TotalsRow.Length ? model.TotalsRow[c] : null;
                    var col  = model.Columns[c];
                    var cell = table.Cell().BorderTop(1).BorderColor(Evergreen).PaddingVertical(5).PaddingHorizontal(5);
                    var text = cell.Text(ReportCellFormatter.Format(raw, col.Type)).Bold().FontSize(8.5f).FontColor(Evergreen);
                    if (col.Type is not ReportColumnType.Text) text.AlignRight();
                }
            }
        });
    }

    // ── Footer: generation timestamp + page numbers ──
    private static void ComposeFooter(IContainer container, ReportModel model)
    {
        container.Column(col =>
        {
            col.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            col.Item().Row(row =>
            {
                row.RelativeItem().Text($"ResQ — generado el {model.GeneratedAt.ToLocalTime():dd/MM/yyyy HH:mm}")
                    .FontSize(7.5f).FontColor(Colors.Grey.Medium);

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Colors.Grey.Medium));
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });
    }
}
