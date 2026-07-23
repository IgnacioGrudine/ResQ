using System.Text;
using QuestPDF.Infrastructure;
using ResQ.API.Models.Reporting;
using ResQ.API.Services.Reporting;
using ResQ.API.Services.Reporting.Implementations;

namespace ResQ.Tests.Services;

/// <summary>
/// <see cref="PdfReportExporter"/> generates a real PDF binary via QuestPDF. Asserting on
/// exact byte content would be brittle and low-value, so these tests confirm the exporter
/// completes without throwing for representative inputs (full model, empty model, edge cases
/// like many KPIs or a totals row) and produces a well-formed, non-empty PDF document.
/// </summary>
public class PdfReportExporterTests
{
    static PdfReportExporterTests()
    {
        // QuestPDF requires an explicit license selection before generating any document.
        // Production wires this up once in Program.cs; the test assembly needs its own.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private readonly PdfReportExporter _sut = new();

    private static ReportModel BuildFullModel() => new()
    {
        Title = "Reporte financiero global — ResQ",
        Subtitle = "01/07/2026 – 23/07/2026",
        GeneratedAt = DateTime.UtcNow,
        Kpis =
        [
            new ReportKpi("Ingresos", "$12.340"),
            new ReportKpi("Órdenes", "58")
        ],
        Columns =
        [
            new ReportColumn { Header = "Comercio", Type = ReportColumnType.Text },
            new ReportColumn { Header = "Ventas", Type = ReportColumnType.Currency },
            new ReportColumn { Header = "Cantidad", Type = ReportColumnType.Number },
            new ReportColumn { Header = "Fecha", Type = ReportColumnType.Date }
        ],
        Rows =
        [
            ["Pastelería Sol", 1500m, 3, new DateTime(2026, 7, 1)],
            ["Café Luna", 980.5m, 2m, new DateTime(2026, 7, 5)]
        ],
        TotalsRow = ["Total", 2480.5m, 5m, null]
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // Format
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Format_IsPdf()
    {
        Assert.Equal(ReportFormat.Pdf, _sut.Format);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Export
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Export_WithFullModel_ReturnsNonEmptyPdfFile()
    {
        // Arrange
        var model = BuildFullModel();

        // Act
        var file = _sut.Export(model, "reporte-financiero");

        // Assert
        Assert.NotEmpty(file.Content);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("reporte-financiero.pdf", file.FileName);
        // PDF files start with the "%PDF" magic header.
        Assert.Equal("%PDF", Encoding.ASCII.GetString(file.Content, 0, 4));
    }

    [Fact]
    public void Export_WithNoColumnsOrKpis_DoesNotThrow()
    {
        // Arrange
        var model = new ReportModel { Title = "Reporte vacío" };

        // Act
        var file = _sut.Export(model, "vacio");

        // Assert
        Assert.NotEmpty(file.Content);
    }

    [Fact]
    public void Export_WithColumnsButNoRows_DoesNotThrow()
    {
        // Arrange
        var model = new ReportModel
        {
            Title = "Sin datos",
            Columns = [new ReportColumn { Header = "Comercio", Type = ReportColumnType.Text }]
        };

        // Act
        var file = _sut.Export(model, "sin-datos");

        // Assert
        Assert.NotEmpty(file.Content);
    }

    [Fact]
    public void Export_WithManyKpis_ChunksIntoMultipleCardRowsWithoutThrowing()
    {
        // Arrange — 7 KPIs exercise the Chunk(3)-based row layout, including the padded last row.
        var model = new ReportModel
        {
            Title = "Reporte con muchos KPIs",
            Kpis = Enumerable.Range(1, 7).Select(i => new ReportKpi($"KPI {i}", i.ToString())).ToList()
        };

        // Act
        var file = _sut.Export(model, "kpis");

        // Assert
        Assert.NotEmpty(file.Content);
    }

    [Fact]
    public void Export_WithTotalsRow_DoesNotThrow()
    {
        // Arrange
        var model = new ReportModel
        {
            Title = "Con totales",
            Columns =
            [
                new ReportColumn { Header = "Comercio", Type = ReportColumnType.Text },
                new ReportColumn { Header = "Ventas", Type = ReportColumnType.Currency }
            ],
            Rows = [["Pastelería Sol", 1500m]],
            TotalsRow = ["Total", 1500m]
        };

        // Act
        var file = _sut.Export(model, "con-totales");

        // Assert
        Assert.NotEmpty(file.Content);
    }

    [Fact]
    public void Export_UsesBaseFileNameForPdfFileName()
    {
        // Arrange
        var model = new ReportModel { Title = "Reporte" };

        // Act
        var file = _sut.Export(model, "mi-reporte-custom");

        // Assert
        Assert.Equal("mi-reporte-custom.pdf", file.FileName);
    }
}
