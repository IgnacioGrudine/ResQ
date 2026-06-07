namespace ResQ.API.DTOs.Merchants;

/// <summary>
/// Data point representing the sales activity for a single day,
/// used to populate the weekly sales chart on the merchant dashboard.
/// </summary>
/// <param name="Day">Human-readable day label (e.g., "Mon", "Tue") or a date string.</param>
/// <param name="Orders">Number of orders completed on this day.</param>
/// <param name="Income">Total revenue earned on this day, in Argentine pesos.</param>
public record DailySalesDto(string Day, int Orders, decimal Income);
