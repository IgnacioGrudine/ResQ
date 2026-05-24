namespace ResQ.API.DTOs.Merchants;

public class MerchantDashboardResponse
{
    // ── Identity ──
    public string BusinessName { get; set; } = string.Empty;

    // ── Today ──
    public int ActiveOrders { get; set; }       // Paid, pending pickup
    public decimal TodayIncome { get; set; }     // earnings from today's paid/picked-up orders
    public int PacksSoldToday { get; set; }

    // ── Historical ──
    public int TotalSales { get; set; }          // total completed (non-cancelled) orders
    public decimal TotalIncome { get; set; }     // accrued earnings
    public decimal KgFoodRescued { get; set; }

    // ── Reputation ──
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }

    // ── Catalog & integrations ──
    public int ActivePackCount { get; set; }
    public string MpConnectionStatus { get; set; } = string.Empty;

    // ── Chart ──
    public List<DailySalesDto> WeeklySales { get; set; } = [];
}
