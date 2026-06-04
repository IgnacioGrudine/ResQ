namespace ResQ.API.DTOs.Orders;

public class CreateOrderRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
