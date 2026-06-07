using ResQ.API.Models.Auth;

namespace ResQ.API.Models.Catalog;

/// <summary>
/// Many-to-many join entity that associates a <see cref="MerchantProfile"/> with a <see cref="Category"/>.
/// A merchant may belong to multiple food categories, and a category may contain multiple merchants.
/// This table has a composite primary key of (<see cref="MerchantId"/>, <see cref="CategoryId"/>).
/// </summary>
public class MerchantCategory
{
    /// <summary>
    /// Foreign key referencing the <see cref="MerchantProfile"/> being tagged with the category.
    /// Part of the composite primary key.
    /// </summary>
    public int MerchantId { get; set; }

    /// <summary>
    /// Foreign key referencing the <see cref="Category"/> assigned to the merchant.
    /// Part of the composite primary key.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Navigation property to the merchant associated with this category assignment.
    /// </summary>
    public MerchantProfile Merchant { get; set; } = null!;

    /// <summary>
    /// Navigation property to the food category assigned to the merchant.
    /// </summary>
    public Category Category { get; set; } = null!;
}
