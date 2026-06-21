namespace ResQ.API.Models.Enums;

/// <summary>
/// Time-bucketing granularity used when aggregating metrics into a time series
/// (e.g. the activity charts on the admin and merchant dashboards).
/// </summary>
public enum ReportGranularity : byte
{
    /// <summary>One data point per calendar day.</summary>
    Day = 1,

    /// <summary>One data point per ISO week (Monday-based).</summary>
    Week = 2,

    /// <summary>One data point per calendar month.</summary>
    Month = 3
}
