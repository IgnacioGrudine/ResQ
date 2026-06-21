using ResQ.API.Models.Enums;
using ResQ.API.Services.Reporting;

namespace ResQ.API.DTOs.Admin;

/// <summary>
/// Query parameters for the admin dashboard and time-series metrics.
/// Bound from the query string; all fields are optional and fall back to sensible defaults
/// (last 30 days, daily granularity) resolved in the service layer.
/// </summary>
public class DashboardQuery
{
    /// <summary>Inclusive start of the reporting window (UTC). Defaults to 30 days ago.</summary>
    public DateTime? From { get; set; }

    /// <summary>Inclusive end of the reporting window (UTC). Defaults to now.</summary>
    public DateTime? To { get; set; }

    /// <summary>Time-bucketing granularity for the activity series. Defaults to <see cref="ReportGranularity.Day"/>.</summary>
    public ReportGranularity Granularity { get; set; } = ReportGranularity.Day;
}

/// <summary>Query parameters for the paginated admin merchant-management list.</summary>
public class MerchantListQuery
{
    /// <summary>Filter by active state; null returns both active and inactive merchants.</summary>
    public bool? Active { get; set; }

    /// <summary>Filter by assigned category ID; null returns all categories.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Filter by Mercado Pago connection status (e.g. "Connected"); null returns all.</summary>
    public string? MpStatus { get; set; }

    /// <summary>1-based page index. Defaults to 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Items per page. Defaults to 10.</summary>
    public int PageSize { get; set; } = 10;
}

/// <summary>Query parameters for the paginated admin user-management list.</summary>
public class UserListQuery
{
    /// <summary>Filter by role; null returns all roles.</summary>
    public Role? Role { get; set; }

    /// <summary>Filter by active state; null returns both active and inactive accounts.</summary>
    public bool? Active { get; set; }

    /// <summary>1-based page index. Defaults to 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Items per page. Defaults to 10.</summary>
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Query parameters for an exportable admin report: the date window and the desired
/// output format (PDF or Excel). The report type is selected via the route.
/// </summary>
public class ReportQuery
{
    /// <summary>Inclusive start of the reporting window (UTC). Defaults to 30 days ago.</summary>
    public DateTime? From { get; set; }

    /// <summary>Inclusive end of the reporting window (UTC). Defaults to now.</summary>
    public DateTime? To { get; set; }

    /// <summary>Output format. Defaults to <see cref="ReportFormat.Pdf"/>.</summary>
    public ReportFormat Format { get; set; } = ReportFormat.Pdf;
}
