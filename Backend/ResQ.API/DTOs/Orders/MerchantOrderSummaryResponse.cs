using ResQ.API.DTOs.Shared;

namespace ResQ.API.DTOs.Orders;

public class MerchantOrderSummaryResponse
{
    public int Id { get; set; }
    public string ConsumerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string PickupCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OrderDetailItemResponse> Items { get; set; } = [];
}
