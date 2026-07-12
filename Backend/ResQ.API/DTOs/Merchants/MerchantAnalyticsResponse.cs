namespace ResQ.API.DTOs.Merchants;

/// <summary>
/// Filterable analytics for a single merchant over a chosen date range. Complements the
/// always-on <see cref="MerchantDashboardResponse"/> with period-scoped KPIs, a growth
/// comparison against the previous equal-length period, top-selling packs, the rating
/// distribution, and an activity time series.
/// </summary>
public class MerchantAnalyticsResponse
{
    /// <summary>Human-readable description of the date range covered.</summary>
    public string RangeLabel { get; set; } = string.Empty;

    // ── 💰 Money (period) ──

    /// <summary>Gross merchandise value in range (Σ TotalAmount over non-cancelled orders).</summary>
    public decimal Gmv { get; set; }

    /// <summary>Net merchant earnings in range (Σ MerchantEarnings over non-cancelled orders).</summary>
    public decimal Earnings { get; set; }

    /// <summary>Average order value in range (GMV / non-cancelled orders).</summary>
    public decimal AverageOrderValue { get; set; }

    /// <summary>Net earnings of the previous equal-length period, for comparison.</summary>
    public decimal PreviousEarnings { get; set; }

    /// <summary>
    /// Percentage change in earnings versus the previous period (can be negative).
    /// <c>null</c> when the previous period had zero earnings — there's no valid baseline
    /// to compute a percentage against, so the frontend shows a "Nuevo" badge instead of a number.
    /// </summary>
    public decimal? EarningsGrowthPct { get; set; }

    // ── 📦 Orders (period) ──

    /// <summary>Completed (PickedUp) orders in range.</summary>
    public int CompletedOrders { get; set; }

    /// <summary>Cancelled orders in range.</summary>
    public int CancelledOrders { get; set; }

    /// <summary>Percentage of in-range orders that were cancelled (0–100).</summary>
    public decimal CancellationRate { get; set; }

    /// <summary>Total packs sold in range (Σ quantities over non-cancelled orders).</summary>
    public int PacksSold { get; set; }

    // ── ⭐ Reputation ──

    /// <summary>Average rating across all reviews (all time).</summary>
    public decimal AverageRating { get; set; }

    /// <summary>Total reviews received (all time).</summary>
    public int ReviewCount { get; set; }

    /// <summary>Count of reviews per star value (index 0 = 1★ … index 4 = 5★).</summary>
    public List<RatingBucketDto> RatingDistribution { get; set; } = [];

    // ── 🏆 Top packs ──

    /// <summary>Best-selling packs in range, ordered by units sold.</summary>
    public List<TopProductDto> TopProducts { get; set; } = [];

    // ── 📈 Series ──

    /// <summary>Earnings/orders time series bucketed by the requested granularity.</summary>
    public List<DailySalesDto> ActivitySeries { get; set; } = [];
}

/// <summary>Number of reviews that gave a particular star rating.</summary>
/// <param name="Stars">The star value (1–5).</param>
/// <param name="Count">How many reviews gave this rating.</param>
public record RatingBucketDto(int Stars, int Count);

/// <summary>A best-selling pack with its sales figures for the period.</summary>
/// <param name="ProductId">The product ID.</param>
/// <param name="Name">The product name.</param>
/// <param name="UnitsSold">Units sold in range.</param>
/// <param name="Revenue">Gross revenue from this pack in range.</param>
public record TopProductDto(int ProductId, string Name, int UnitsSold, decimal Revenue);
