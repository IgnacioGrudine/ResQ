namespace ResQ.API.Models.Reporting;

/// <summary>
/// Format hint for a report column, driving how each cell is rendered by the
/// PDF and Excel exporters (alignment, number formatting, currency symbol, etc.).
/// </summary>
public enum ReportColumnType
{
    /// <summary>Plain left-aligned text.</summary>
    Text,

    /// <summary>Right-aligned integer or decimal number with thousands separators.</summary>
    Number,

    /// <summary>Right-aligned monetary value rendered in Argentine pesos (ARS).</summary>
    Currency,

    /// <summary>A date rendered as <c>dd/MM/yyyy</c>.</summary>
    Date
}

/// <summary>
/// Describes a single column of a tabular report: its header text and the
/// formatting/alignment behaviour applied to every cell beneath it.
/// </summary>
public class ReportColumn
{
    /// <summary>The column header shown in the table.</summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>The data type that controls cell formatting and alignment.</summary>
    public ReportColumnType Type { get; set; } = ReportColumnType.Text;

    /// <summary>
    /// Relative width weight for the column (used by the PDF exporter to distribute
    /// horizontal space). Defaults to 1.
    /// </summary>
    public float Width { get; set; } = 1f;
}

/// <summary>
/// A headline metric rendered as a summary card at the top of a report
/// (e.g. "Ingresos de plataforma: $12.340"). Independent of the tabular data.
/// </summary>
public class ReportKpi
{
    /// <summary>Initialises a KPI with its label and pre-formatted display value.</summary>
    public ReportKpi(string label, string value)
    {
        Label = label;
        Value = value;
    }

    /// <summary>Human-readable label describing the metric.</summary>
    public string Label { get; set; }

    /// <summary>The pre-formatted value to display (the service decides formatting).</summary>
    public string Value { get; set; }
}

/// <summary>
/// Format-agnostic representation of a report. A single <see cref="ReportModel"/> is
/// produced by a service and handed to either the PDF or the Excel exporter, so the
/// reporting logic is written once and reused across both output formats.
/// </summary>
/// <remarks>
/// Cell values in <see cref="Rows"/> and <see cref="TotalsRow"/> are stored as raw
/// <see cref="object"/> values (e.g. <see cref="decimal"/>, <see cref="int"/>,
/// <see cref="DateTime"/>, <see cref="string"/>) so the exporters can apply the correct
/// formatting per <see cref="ReportColumn.Type"/> — giving real numeric cells in Excel
/// and consistently formatted text in the PDF.
/// </remarks>
public class ReportModel
{
    /// <summary>Main report title (e.g. "Reporte financiero global — ResQ").</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional secondary line, typically the date range covered by the report.</summary>
    public string? Subtitle { get; set; }

    /// <summary>UTC timestamp at which the report was generated.</summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Headline KPIs rendered as summary cards above the table.</summary>
    public List<ReportKpi> Kpis { get; set; } = [];

    /// <summary>The ordered column definitions for the tabular section.</summary>
    public List<ReportColumn> Columns { get; set; } = [];

    /// <summary>The data rows; each row's cell count must match <see cref="Columns"/>.</summary>
    public List<object?[]> Rows { get; set; } = [];

    /// <summary>
    /// Optional totals row rendered with emphasis at the foot of the table.
    /// Cell count must match <see cref="Columns"/>; use <c>null</c> cells for columns
    /// that should not show a total.
    /// </summary>
    public object?[]? TotalsRow { get; set; }
}
