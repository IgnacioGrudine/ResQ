using ResQ.API.Models.Common;

namespace ResQ.API.Models.Catalog;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<MerchantCategory> MerchantCategories { get; set; } = [];
}
