using ResQ.API.Models.Auth;
using ResQ.API.Models.Common;
using ResQ.API.Models.Orders;

namespace ResQ.API.Models.Reviews;

public class Review : BaseEntity
{
    public int OrderId { get; set; }
    public int MerchantId { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }

    public Order Order { get; set; } = null!;
    public MerchantProfile Merchant { get; set; } = null!;
}
