using ResQ.API.Models.Reporting;

namespace ResQ.API.Services.Reporting;

/// <summary>
/// Renders a format-agnostic <see cref="ReportModel"/> into a concrete file format.
/// Two implementations exist — one for PDF (QuestPDF) and one for Excel (ClosedXML) —
/// both consuming the same model so reporting logic is never duplicated per format.
/// </summary>
public interface IReportExporter
{
    /// <summary>The output format this exporter produces.</summary>
    ReportFormat Format { get; }

    /// <summary>
    /// Renders the given model and returns the raw file bytes together with the
    /// matching content-type and a filename derived from <paramref name="baseFileName"/>.
    /// </summary>
    /// <param name="model">The report content to render.</param>
    /// <param name="baseFileName">Filename without extension; the exporter appends the correct one.</param>
    /// <returns>The rendered <see cref="ReportFile"/>.</returns>
    ReportFile Export(ReportModel model, string baseFileName);
}

/// <summary>
/// Resolves the appropriate <see cref="IReportExporter"/> for a requested
/// <see cref="ReportFormat"/> and renders a <see cref="ReportModel"/> in one call.
/// Lets services produce a report without referencing a specific exporter.
/// </summary>
public interface IReportFileFactory
{
    /// <summary>
    /// Renders <paramref name="model"/> in the requested <paramref name="format"/>.
    /// </summary>
    /// <param name="model">The report content to render.</param>
    /// <param name="format">The desired output format.</param>
    /// <param name="baseFileName">Filename without extension.</param>
    /// <returns>The rendered <see cref="ReportFile"/>.</returns>
    ReportFile Create(ReportModel model, ReportFormat format, string baseFileName);
}
