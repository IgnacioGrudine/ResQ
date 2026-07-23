using System.Globalization;
using System.Reflection;
using ResQ.API.Models.Reporting;
using ResQ.API.Services.Reporting.Implementations;

namespace ResQ.Tests.Services;

/// <summary>
/// <see cref="ReportCellFormatter"/> is an <c>internal static</c> class with no
/// <c>InternalsVisibleTo</c> exposure to this test assembly, so its formatting method is
/// invoked here via reflection rather than a direct call. This does not touch production
/// code — it only reaches into the already-compiled ResQ.API assembly from the test.
/// </summary>
public class ReportCellFormatterTests
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-AR");

    private static readonly MethodInfo FormatMethod =
        typeof(ExcelReportExporter).Assembly
            .GetType("ResQ.API.Services.Reporting.Implementations.ReportCellFormatter")!
            .GetMethod("Format", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static string Format(object? value, ReportColumnType type) =>
        (string)FormatMethod.Invoke(null, [value, type])!;

    // ═══════════════════════════════════════════════════════════════════════════
    // Null handling
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ReportColumnType.Text)]
    [InlineData(ReportColumnType.Number)]
    [InlineData(ReportColumnType.Currency)]
    [InlineData(ReportColumnType.Date)]
    public void Format_WhenValueIsNull_ReturnsEmptyString(ReportColumnType type)
    {
        // Act
        var result = Format(null, type);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // String literal short-circuit
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Format_WhenValueIsString_ReturnsLiteralUnchangedForTextColumn()
    {
        // Act
        var result = Format("Total", ReportColumnType.Text);

        // Assert
        Assert.Equal("Total", result);
    }

    [Theory]
    [InlineData(ReportColumnType.Currency)]
    [InlineData(ReportColumnType.Number)]
    [InlineData(ReportColumnType.Date)]
    public void Format_WhenValueIsStringInTypedColumn_ReturnsLiteralIgnoringColumnType(ReportColumnType type)
    {
        // Act — e.g. a "Total" row-header label stored in a Currency/Number/Date column.
        var result = Format("Total", type);

        // Assert
        Assert.Equal("Total", result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Currency
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Format_Currency_PrefixesWithDollarSignAndUsesThousandsSeparator()
    {
        // Act
        var result = Format(12340m, ReportColumnType.Currency);

        // Assert
        Assert.Equal("$" + 12340m.ToString("N0", Culture), result);
        Assert.StartsWith("$", result);
    }

    [Fact]
    public void Format_Currency_WithIntValue_ConvertsToDecimalFirst()
    {
        // Act
        var result = Format(500, ReportColumnType.Currency);

        // Assert
        Assert.Equal("$" + 500m.ToString("N0", Culture), result);
    }

    [Fact]
    public void Format_Currency_WithZero_FormatsAsDollarZero()
    {
        // Act
        var result = Format(0m, ReportColumnType.Currency);

        // Assert
        Assert.Equal("$" + 0m.ToString("N0", Culture), result);
    }

    [Fact]
    public void Format_Currency_RoundsToWholeNumber()
    {
        // Act
        var result = Format(1500.75m, ReportColumnType.Currency);

        // Assert
        Assert.Equal("$" + 1500.75m.ToString("N0", Culture), result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Number
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Format_Number_WhenDecimal_FormatsWithOneDecimalPlace()
    {
        // Act
        var result = Format(1234.56m, ReportColumnType.Number);

        // Assert
        Assert.Equal(1234.56m.ToString("N1", Culture), result);
    }

    [Fact]
    public void Format_Number_WhenInt_FormatsWithoutDecimalPlaces()
    {
        // Act
        var result = Format(1234, ReportColumnType.Number);

        // Assert
        Assert.Equal(1234m.ToString("N0", Culture), result);
    }

    [Fact]
    public void Format_Number_WhenNegativeDecimal_FormatsWithSign()
    {
        // Act
        var result = Format(-42.1m, ReportColumnType.Number);

        // Assert
        Assert.Equal((-42.1m).ToString("N1", Culture), result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Date
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Format_Date_FormatsAsDdMmYyyy()
    {
        // Act
        var result = Format(new DateTime(2026, 7, 23), ReportColumnType.Date);

        // Assert
        Assert.Equal("23/07/2026", result);
    }

    [Fact]
    public void Format_Date_PadsSingleDigitDayAndMonth()
    {
        // Act
        var result = Format(new DateTime(2026, 1, 5), ReportColumnType.Date);

        // Assert
        Assert.Equal("05/01/2026", result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Text / default
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Format_Text_ReturnsToStringOfValue()
    {
        // Act
        var result = Format(42, ReportColumnType.Text);

        // Assert
        Assert.Equal("42", result);
    }
}
