namespace ResQ.API.DTOs.Products;

public class ProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public TimeOnly PickupTimeStart { get; set; }
    public TimeOnly PickupTimeEnd { get; set; }
    public bool IsActive { get; set; }
}
