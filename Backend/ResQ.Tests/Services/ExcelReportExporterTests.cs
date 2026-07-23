using ResQ.API.Models.Reporting;
using ResQ.API.Services.Reporting;
using ResQ.API.Services.Reporting.Implementations;

namespace ResQ.Tests.Services;

/// <summary>
/// <see cref="ExcelReportExporter"/> generates a real .xlsx binary via ClosedXML. Asserting on
/// exact byte content would be brittle and low-value, so these tests confirm the exporter
/// completes without throwing for representative inputs (full model, empty model, edge cases)
/// and produces a well-formed, non-empty Office Open XML package.
/// </summary>
public class ExcelReportExporterTests
{
    private readonly ExcelReportExporter _sut = new();

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
    public void Format_IsExcel()
    {
        Assert.Equal(ReportFormat.Excel, _sut.Format);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Export
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Export_WithFullModel_ReturnsNonEmptyXlsxFile()
    {
        // Arrange
        var model = BuildFullModel();

        // Act
        var file = _sut.Export(model, "reporte-financiero");

        // Assert
        Assert.NotEmpty(file.Content);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        Assert.Equal("reporte-financiero.xlsx", file.FileName);
        // .xlsx files are ZIP archives — verify the "PK" local file header signature.
        Assert.Equal(0x50, file.Content[0]);
        Assert.Equal(0x4B, file.Content[1]);
    }

    [Fact]
    public void Export_WithNoColumnsRowsOrKpis_DoesNotThrowAndReturnsNonEmptyFile()
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
    public void Export_WithoutSubtitleOrKpis_DoesNotThrow()
    {
        // Arrange
        var model = new ReportModel
        {
            Title = "Reporte simple",
            Columns = [new ReportColumn { Header = "Comercio", Type = ReportColumnType.Text }],
            Rows = [["Solo texto"]]
        };

        // Act
        var file = _sut.Export(model, "simple");

        // Assert
        Assert.NotEmpty(file.Content);
    }

    [Fact]
    public void Export_UsesBaseFileNameForXlsxFileName()
    {
        // Arrange
        var model = new ReportModel { Title = "Reporte" };

        // Act
        var file = _sut.Export(model, "mi-reporte-custom");

        // Assert
        Assert.Equal("mi-reporte-custom.xlsx", file.FileName);
    }
}
