using ResQ.API.Models.Auth;
using ResQ.API.Models.Common;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Orders;

namespace ResQ.API.Models.Catalog;

/// <summary>
/// Represents a product listing published by a merchant on the ResQ platform.
/// Typically a Surprise Pack — a discounted bag of surplus food whose exact contents
/// are revealed at pickup — though explicit items may also be listed.
/// </summary>
public class Product : BaseEntity
{
    /// <summary>
    /// Foreign key referencing the <see cref="MerchantProfile"/> that owns and published this product.
    /// </summary>
    public int MerchantId { get; set; }

    /// <summary>
    /// Display name of the product shown to consumers when browsing (e.g. "Surprise Bakery Pack").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description providing additional details about the product,
    /// such as typical contents, allergen information, or serving size.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL of the product image stored in MinIO object storage.
    /// Null if the merchant has not uploaded a photo for this product.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Classification of the product — whether it is a Surprise Pack or an explicitly described item.
    /// </summary>
    public ProductType ProductType { get; set; }

    /// <summary>
    /// The regular retail price of the product before the ResQ discount is applied.
    /// Displayed alongside <see cref="SalePrice"/> to communicate the savings to the consumer.
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// The discounted price at which the product is sold through the ResQ platform.
    /// This is the amount charged to the consumer via Mercado Pago.
    /// </summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Number of units currently available for purchase. Decremented as orders are placed.
    /// When zero, the product is unavailable for new orders.
    /// </summary>
    public int StockQuantity { get; set; } = 0;

    /// <summary>
    /// The earliest time of day at which the consumer may arrive at the merchant to collect this product.
    /// </summary>
    public TimeOnly PickupTimeStart { get; set; }

    /// <summary>
    /// The latest time of day by which the consumer must arrive at the merchant to collect this product.
    /// </summary>
    public TimeOnly PickupTimeEnd { get; set; }

    /// <summary>
    /// Indicates whether this product is currently listed and visible to consumers.
    /// Merchants can deactivate a product without deleting it.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property to the merchant who published this product.
    /// </summary>
    public MerchantProfile Merchant { get; set; } = null!;

    /// <summary>
    /// All order line items that include this product.
    /// </summary>
    public ICollection<OrderDetail> OrderDetails { get; set; } = [];
}
