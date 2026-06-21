using ResQ.API.Models.Reporting;

namespace ResQ.API.Services.Reporting.Implementations;

/// <summary>
/// Resolves the registered <see cref="IReportExporter"/> matching a requested
/// <see cref="ReportFormat"/> and delegates rendering to it. Injected wherever a service
/// needs to produce a downloadable report without binding to a specific format.
/// </summary>
public class ReportFileFactory(IEnumerable<IReportExporter> exporters) : IReportFileFactory
{
    private readonly IReadOnlyDictionary<ReportFormat, IReportExporter> _exporters =
        exporters.ToDictionary(e => e.Format);

    /// <inheritdoc />
    public ReportFile Create(ReportModel model, ReportFormat format, string baseFileName)
    {
        if (!_exporters.TryGetValue(format, out var exporter))
            throw new NotSupportedException($"No hay un exportador registrado para el formato {format}.");

        return exporter.Export(model, baseFileName);
    }
}
