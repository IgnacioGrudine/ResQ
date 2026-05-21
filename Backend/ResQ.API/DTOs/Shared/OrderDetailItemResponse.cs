namespace ResQ.API.DTOs.Shared;

public class OrderDetailItemResponse
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
