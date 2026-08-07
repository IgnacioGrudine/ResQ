using ClosedXML.Excel;
using ResQ.API.Models.Reporting;

namespace ResQ.API.Services.Reporting.Implementations;

/// <summary>
/// Renders a <see cref="ReportModel"/> to an Office Open XML (.xlsx) spreadsheet using ClosedXML.
/// Numeric and currency columns are written as real numbers with Excel number formats so the
/// recipient can sort, filter, and run formulas over them.
/// </summary>
public class ExcelReportExporter : IReportExporter
{
    /// <inheritdoc />
    public ReportFormat Format => ReportFormat.Excel;

    // ResQ brand palette
    private static readonly XLColor Evergreen = XLColor.FromHtml("#1B3A2F");
    private static readonly XLColor Lime      = XLColor.FromHtml("#D4E8B8");

    /// <inheritdoc />
    public ReportFile Export(ReportModel model, string baseFileName)
    {
        using var workbook  = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Reporte");

        var columnCount = Math.Max(model.Columns.Count, 1);
        var row = 1;

        // ── Title ──
        var titleCell = worksheet.Cell(row, 1);
        titleCell.Value = model.Title;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 16;
        titleCell.Style.Font.FontColor = Evergreen;
        worksheet.Range(row, 1, row, columnCount).Merge();
        row++;

        // ── Subtitle (date range) ──
        if (!string.IsNullOrWhiteSpace(model.Subtitle))
        {
            var sub = worksheet.Cell(row, 1);
            sub.Value = model.Subtitle;
            sub.Style.Font.FontColor = XLColor.Gray;
            worksheet.Range(row, 1, row, columnCount).Merge();
            row++;
        }

        // ── Generated-at line ──
        var genCell = worksheet.Cell(row, 1);
        genCell.Value = $"Generado el {model.GeneratedAt.ToLocalTime():dd/MM/yyyy HH:mm}";
        genCell.Style.Font.FontSize = 9;
        genCell.Style.Font.FontColor = XLColor.Gray;
        worksheet.Range(row, 1, row, columnCount).Merge();
        row += 2;

        // ── KPI cards (label / value pairs) ──
        if (model.Kpis.Count > 0)
        {
            foreach (var kpi in model.Kpis)
            {
                var label = worksheet.Cell(row, 1);
                label.Value = kpi.Label;
                label.Style.Font.Bold = true;
                label.Style.Fill.BackgroundColor = Lime;

                var value = worksheet.Cell(row, 2);
                value.Value = kpi.Value;
                value.Style.Fill.BackgroundColor = Lime;
                row++;
            }
            row++;
        }

        // ── Table header ──
        if (model.Columns.Count > 0)
        {
            var headerRow = row;
            for (var c = 0; c < model.Columns.Count; c++)
            {
                var cell = worksheet.Cell(headerRow, c + 1);
                cell.Value = model.Columns[c].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = Evergreen;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            row++;

            // ── Data rows ──
            foreach (var dataRow in model.Rows)
            {
                for (var c = 0; c < model.Columns.Count; c++)
                {
                    var raw = c < dataRow.Length ? dataRow[c] : null;
                    WriteCell(worksheet.Cell(row, c + 1), raw, model.Columns[c].Type);
                }
                row++;
            }

            // ── Totals row ──
            if (model.TotalsRow is not null)
            {
                for (var c = 0; c < model.Columns.Count; c++)
                {
                    var raw  = c < model.TotalsRow.Length ? model.TotalsRow[c] : null;
                    var cell = worksheet.Cell(row, c + 1);
                    WriteCell(cell, raw, model.Columns[c].Type);
                    cell.Style.Font.Bold = true;
                    cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                }
                row++;
            }

            worksheet.Columns().AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ReportFile(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{baseFileName}.xlsx");
    }

    /// <summary>
    /// Writes a single value into a cell, using a real numeric value plus an Excel number
    /// format for <see cref="ReportColumnType.Number"/>/<see cref="ReportColumnType.Currency"/>,
    /// a date value for <see cref="ReportColumnType.Date"/>, and text otherwise.
    /// </summary>
    private static void WriteCell(IXLCell cell, object? raw, ReportColumnType type)
    {
        if (raw is null) { cell.Value = string.Empty; return; }

        // A string in a typed column is always a literal label (e.g. the "Total" row header).
        if (raw is string literal) { cell.Value = literal; return; }

        switch (type)
        {
            case ReportColumnType.Currency:
                cell.Value = Convert.ToDecimal(raw, ReportCellFormatter.Culture);
                // "#.##" (vs. a bare "#,##0") only renders decimal digits when they're
                // actually significant, so a $0.10 platform fee shows as "$0.1" instead of
                // silently truncating to "$0", while whole amounts still print with no
                // decimal point at all.
                cell.Style.NumberFormat.Format = "\"$\"#,##0.##";
                break;
            case ReportColumnType.Number:
                cell.Value = Convert.ToDecimal(raw, ReportCellFormatter.Culture);
                cell.Style.NumberFormat.Format = raw is decimal ? "#,##0.0" : "#,##0";
                break;
            case ReportColumnType.Date:
                cell.Value = Convert.ToDateTime(raw, ReportCellFormatter.Culture);
                cell.Style.NumberFormat.Format = "dd/mm/yyyy";
                break;
            default:
                cell.Value = raw.ToString();
                break;
        }
    }
}
