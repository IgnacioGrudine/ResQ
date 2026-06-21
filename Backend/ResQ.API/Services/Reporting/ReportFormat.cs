namespace ResQ.API.Services.Reporting;

/// <summary>
/// The output format requested for an exportable report.
/// Bound from the <c>format</c> query-string parameter on report endpoints.
/// </summary>
public enum ReportFormat
{
    /// <summary>Portable Document Format, generated with QuestPDF.</summary>
    Pdf,

    /// <summary>Office Open XML spreadsheet (.xlsx), generated with ClosedXML.</summary>
    Excel
}

/// <summary>
/// The fully-rendered output of a report export: the binary content together with
/// the HTTP <c>Content-Type</c> and a suggested download filename. Returned by the
/// service layer and handed straight to <c>File(...)</c> in the controller.
/// </summary>
/// <param name="Content">The raw file bytes (PDF or XLSX).</param>
/// <param name="ContentType">The MIME type matching <paramref name="Content"/>.</param>
/// <param name="FileName">The suggested download filename, including extension.</param>
public record ReportFile(byte[] Content, string ContentType, string FileName);
