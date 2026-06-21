using System.Globalization;
using ResQ.API.Models.Reporting;

namespace ResQ.API.Services.Reporting.Implementations;

/// <summary>
/// Shared formatting helpers that turn raw report cell values into display strings
/// according to their <see cref="ReportColumnType"/>. Used by the PDF exporter (which
/// always renders text) and by the Excel exporter for non-numeric cells.
/// </summary>
internal static class ReportCellFormatter
{
    /// <summary>Argentine culture, so currency and numbers use "." for thousands and "," for decimals.</summary>
    internal static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-AR");

    /// <summary>
    /// Formats a raw cell value as a display string based on the column type.
    /// Null values render as an empty string.
    /// </summary>
    internal static string Format(object? value, ReportColumnType type)
    {
        if (value is null) return string.Empty;

        // A string in a typed column is always a literal label (e.g. the "Total" row header).
        if (value is string literal) return literal;

        return type switch
        {
            ReportColumnType.Currency => "$" + Convert.ToDecimal(value, Culture).ToString("N0", Culture),
            ReportColumnType.Number   => value is decimal d
                                            ? d.ToString("N1", Culture)
                                            : Convert.ToDecimal(value, Culture).ToString("N0", Culture),
            ReportColumnType.Date     => Convert.ToDateTime(value, Culture).ToString("dd/MM/yyyy", Culture),
            _                         => value.ToString() ?? string.Empty
        };
    }
}
