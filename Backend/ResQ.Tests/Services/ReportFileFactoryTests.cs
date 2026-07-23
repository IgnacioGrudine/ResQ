using Moq;
using ResQ.API.Models.Reporting;
using ResQ.API.Services.Reporting;
using ResQ.API.Services.Reporting.Implementations;

namespace ResQ.Tests.Services;

public class ReportFileFactoryTests
{
    private readonly Mock<IReportExporter> _pdfExporter = new();
    private readonly Mock<IReportExporter> _excelExporter = new();

    private static ReportFileFactory CreateSut(params IReportExporter[] exporters) => new(exporters);

    private static ReportModel BuildModel() => new() { Title = "Reporte de prueba" };

    // ═══════════════════════════════════════════════════════════════════════════
    // Create
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Create_WhenFormatIsRegistered_DelegatesToMatchingExporter()
    {
        // Arrange
        var model = BuildModel();
        var expectedFile = new ReportFile([1, 2, 3], "application/pdf", "reporte.pdf");
        _pdfExporter.SetupGet(e => e.Format).Returns(ReportFormat.Pdf);
        _pdfExporter.Setup(e => e.Export(model, "reporte")).Returns(expectedFile);
        _excelExporter.SetupGet(e => e.Format).Returns(ReportFormat.Excel);

        var sut = CreateSut(_pdfExporter.Object, _excelExporter.Object);

        // Act
        var result = sut.Create(model, ReportFormat.Pdf, "reporte");

        // Assert
        Assert.Same(expectedFile, result);
        _pdfExporter.Verify(e => e.Export(model, "reporte"), Times.Once);
        _excelExporter.Verify(e => e.Export(It.IsAny<ReportModel>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Create_WithMultipleExportersRegistered_PicksCorrectOneForEachFormat()
    {
        // Arrange
        var pdfFile = new ReportFile([1], "application/pdf", "a.pdf");
        var excelFile = new ReportFile([2], "application/vnd.ms-excel", "a.xlsx");
        _pdfExporter.SetupGet(e => e.Format).Returns(ReportFormat.Pdf);
        _pdfExporter.Setup(e => e.Export(It.IsAny<ReportModel>(), It.IsAny<string>())).Returns(pdfFile);
        _excelExporter.SetupGet(e => e.Format).Returns(ReportFormat.Excel);
        _excelExporter.Setup(e => e.Export(It.IsAny<ReportModel>(), It.IsAny<string>())).Returns(excelFile);

        var sut = CreateSut(_pdfExporter.Object, _excelExporter.Object);

        // Act
        var pdfResult = sut.Create(BuildModel(), ReportFormat.Pdf, "a");
        var excelResult = sut.Create(BuildModel(), ReportFormat.Excel, "a");

        // Assert
        Assert.Same(pdfFile, pdfResult);
        Assert.Same(excelFile, excelResult);
    }

    [Fact]
    public void Create_WhenFormatIsNotRegistered_ThrowsNotSupportedException()
    {
        // Arrange
        _pdfExporter.SetupGet(e => e.Format).Returns(ReportFormat.Pdf);
        var sut = CreateSut(_pdfExporter.Object);

        // Act & Assert
        var ex = Assert.Throws<NotSupportedException>(() => sut.Create(BuildModel(), ReportFormat.Excel, "reporte"));
        Assert.Contains("Excel", ex.Message);
    }

    [Fact]
    public void Create_WhenNoExportersRegistered_ThrowsNotSupportedException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => sut.Create(BuildModel(), ReportFormat.Pdf, "reporte"));
    }
}
