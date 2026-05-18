using ResQ.API.Models.Auth;

namespace ResQ.API.Models.Catalog;

public class MerchantCategory
{
    public int MerchantId { get; set; }
    public int CategoryId { get; set; }

    public MerchantProfile Merchant { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
