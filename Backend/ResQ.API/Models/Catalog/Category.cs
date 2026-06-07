using ResQ.API.Models.Common;

namespace ResQ.API.Models.Catalog;

/// <summary>
/// Represents a food category (e.g. Bakery, Sushi, Coffee, Pizza) used to classify merchants
/// and help consumers filter the platform by cuisine type.
/// Categories are platform-level entities managed by administrators.
/// </summary>
public class Category : BaseEntity
{
    /// <summary>
    /// Human-readable name of the food category, displayed to consumers on the browse screen.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The merchants that have been tagged with this category via the many-to-many
    /// join table <see cref="MerchantCategory"/>.
    /// </summary>
    public ICollection<MerchantCategory> MerchantCategories { get; set; } = [];
}
