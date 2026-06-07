namespace ResQ.API.DTOs.Merchants;

/// <summary>
/// Response returned by the merchant dashboard endpoint, aggregating identity,
/// real-time activity, historical performance, reputation, catalog, and chart data
/// for the authenticated merchant.
/// </summary>
public class MerchantDashboardResponse
{
    // ── Identity ──

    /// <summary>
    /// Public-facing business name of the merchant.
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    // ── Today ──

    /// <summary>
    /// Number of orders that have been paid and are currently awaiting pickup today.
    /// </summary>
    public int ActiveOrders { get; set; }       // Paid, pending pickup

    /// <summary>
    /// Total revenue from today's paid and/or picked-up orders, in Argentine pesos.
    /// </summary>
    public decimal TodayIncome { get; set; }     // earnings from today's paid/picked-up orders

    /// <summary>
    /// Number of packs sold today.
    /// </summary>
    public int PacksSoldToday { get; set; }

    // ── Historical ──

    /// <summary>
    /// Total number of completed (non-cancelled) orders across all time.
    /// </summary>
    public int TotalSales { get; set; }          // total completed (non-cancelled) orders

    /// <summary>
    /// Cumulative revenue from all completed orders, in Argentine pesos.
    /// </summary>
    public decimal TotalIncome { get; set; }     // accrued earnings

    /// <summary>
    /// Estimated kilograms of food rescued (not discarded) through ResQ sales.
    /// </summary>
    public decimal KgFoodRescued { get; set; }

    // ── Reputation ──

    /// <summary>
    /// Average star rating of the merchant computed from all reviews received.
    /// </summary>
    public decimal AverageRating { get; set; }

    /// <summary>
    /// Total number of reviews the merchant has received.
    /// </summary>
    public int ReviewCount { get; set; }

    // ── Catalog & integrations ──

    /// <summary>
    /// Number of products (packs) currently marked as active in the merchant's catalog.
    /// </summary>
    public int ActivePackCount { get; set; }

    /// <summary>
    /// Current Mercado Pago connection status for the merchant (e.g., "Connected", "Disconnected").
    /// </summary>
    public string MpConnectionStatus { get; set; } = string.Empty;

    // ── Chart ──

    /// <summary>
    /// Sales data for the past seven days, used to render the weekly income/orders chart.
    /// </summary>
    public List<DailySalesDto> WeeklySales { get; set; } = [];
}
