using ResQ.API.Models.Enums;

namespace ResQ.API.DTOs.Products;

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public ProductType ProductType { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public TimeOnly PickupTimeStart { get; set; }
    public TimeOnly PickupTimeEnd { get; set; }
}
