using ResQ.API.Models.Auth;
using ResQ.API.Models.Common;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Orders;

namespace ResQ.API.Models.Catalog;

public class Product : BaseEntity
{
    public int MerchantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public ProductType ProductType { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; } = 0;
    public TimeOnly PickupTimeStart { get; set; }
    public TimeOnly PickupTimeEnd { get; set; }
    public bool IsActive { get; set; } = true;

    public MerchantProfile Merchant { get; set; } = null!;
    public ICollection<OrderDetail> OrderDetails { get; set; } = [];
}
