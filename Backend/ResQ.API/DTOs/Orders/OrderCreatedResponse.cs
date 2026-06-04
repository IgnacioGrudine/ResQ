namespace ResQ.API.DTOs.Orders;

public class OrderCreatedResponse
{
    public int OrderId { get; set; }
    public string MpPreferenceId { get; set; } = string.Empty;
    public string MpCheckoutUrl { get; set; } = string.Empty;
}
