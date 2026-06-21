namespace ResQ.API.DTOs.Admin;

/// <summary>
/// Aggregated platform-wide metrics returned by the admin dashboard endpoint.
/// Covers money, orders, merchants, consumers, reputation, time series, rankings,
/// category distribution, and operational alerts — all scoped to the requested date range.
/// No environmental-impact metrics are included; the platform reports on money, users,
/// merchants, and orders only.
/// </summary>
public class AdminDashboardResponse
{
    /// <summary>Human-readable description of the date range covered (e.g. "01/06/2026 — 21/06/2026").</summary>
    public string RangeLabel { get; set; } = string.Empty;

    // ── 💰 Money ──

    /// <summary>Gross Merchandise Value: sum of <c>TotalAmount</c> over completed orders in range.</summary>
    public decimal Gmv { get; set; }

    /// <summary>Platform revenue: sum of <c>PlatformFee</c> over completed orders in range.</summary>
    public decimal PlatformRevenue { get; set; }

    /// <summary>Merchant earnings: sum of <c>MerchantEarnings</c> over completed orders in range.</summary>
    public decimal MerchantEarnings { get; set; }

    /// <summary>Average order value: <see cref="Gmv"/> divided by completed orders in range.</summary>
    public decimal AverageOrderValue { get; set; }

    // ── 📦 Orders ──

    /// <summary>Total orders created in range (any status).</summary>
    public int TotalOrders { get; set; }

    /// <summary>Orders in range that reached the PickedUp state.</summary>
    public int CompletedOrders { get; set; }

    /// <summary>Orders in range that were cancelled.</summary>
    public int CancelledOrders { get; set; }

    /// <summary>Percentage of in-range orders that were cancelled (0–100).</summary>
    public decimal CancellationRate { get; set; }

    /// <summary>Percentage of paid-or-completed in-range orders that were actually picked up (0–100).</summary>
    public decimal PickupRate { get; set; }

    // ── 🏪 Merchants ──

    /// <summary>Total merchants registered on the platform (all time).</summary>
    public int TotalMerchants { get; set; }

    /// <summary>Merchants whose account is currently active.</summary>
    public int ActiveMerchants { get; set; }

    /// <summary>Merchants with a connected Mercado Pago account.</summary>
    public int MerchantsWithMp { get; set; }

    /// <summary>Merchants that registered within the requested range.</summary>
    public int NewMerchants { get; set; }

    // ── 👥 Consumers ──

    /// <summary>Total consumers registered on the platform (all time).</summary>
    public int TotalConsumers { get; set; }

    /// <summary>Consumers whose account is currently active.</summary>
    public int ActiveConsumers { get; set; }

    /// <summary>Consumers that registered within the requested range.</summary>
    public int NewConsumers { get; set; }

    // ── ⭐ Reputation ──

    /// <summary>Average star rating across all reviews on the platform.</summary>
    public decimal AverageRating { get; set; }

    /// <summary>Total number of reviews on the platform.</summary>
    public int ReviewCount { get; set; }

    // ── 📦 Catalog ──

    /// <summary>Number of currently active packs across all merchants.</summary>
    public int ActivePacks { get; set; }

    // ── 📈 Series & breakdowns ──

    /// <summary>Activity time series bucketed by the requested granularity.</summary>
    public List<ActivityPointDto> ActivitySeries { get; set; } = [];

    /// <summary>Top merchants ranked by platform revenue generated in range.</summary>
    public List<MerchantRankingDto> TopMerchantsByRevenue { get; set; } = [];

    /// <summary>Top merchants ranked by number of completed orders in range.</summary>
    public List<MerchantRankingDto> TopMerchantsByOrders { get; set; } = [];

    /// <summary>Distribution of orders and GMV across food categories.</summary>
    public List<CategorySliceDto> CategoryDistribution { get; set; } = [];

    /// <summary>Operational alerts requiring admin attention (MP tokens, etc.).</summary>
    public List<AdminAlertDto> Alerts { get; set; } = [];
}

/// <summary>A single bucket of the activity time series.</summary>
/// <param name="Label">Bucket label (date, ISO week, or month depending on granularity).</param>
/// <param name="Orders">Completed orders in the bucket.</param>
/// <param name="Gmv">Gross merchandise value in the bucket.</param>
/// <param name="PlatformRevenue">Platform fee revenue in the bucket.</param>
public record ActivityPointDto(string Label, int Orders, decimal Gmv, decimal PlatformRevenue);

/// <summary>A merchant's position in a ranking, with the metrics it is ranked by.</summary>
/// <param name="MerchantId">The merchant profile ID.</param>
/// <param name="BusinessName">The merchant's public business name.</param>
/// <param name="Orders">Completed orders attributed to the merchant in range.</param>
/// <param name="Gmv">Gross merchandise value attributed to the merchant in range.</param>
/// <param name="PlatformRevenue">Platform fee revenue attributed to the merchant in range.</param>
/// <param name="AverageRating">The merchant's average rating across all reviews.</param>
public record MerchantRankingDto(
    int MerchantId, string BusinessName, int Orders, decimal Gmv, decimal PlatformRevenue, decimal AverageRating);

/// <summary>Orders and GMV attributed to one food category.</summary>
/// <param name="Category">Category name.</param>
/// <param name="Orders">Completed orders attributed to the category in range.</param>
/// <param name="Gmv">Gross merchandise value attributed to the category in range.</param>
public record CategorySliceDto(string Category, int Orders, decimal Gmv);

/// <summary>An operational alert surfaced on the admin dashboard.</summary>
/// <param name="Severity">Severity level: "info", "warning", or "critical".</param>
/// <param name="Title">Short alert headline.</param>
/// <param name="Detail">Longer description with the specifics.</param>
/// <param name="MerchantId">The related merchant, when the alert concerns one.</param>
public record AdminAlertDto(string Severity, string Title, string Detail, int? MerchantId);
