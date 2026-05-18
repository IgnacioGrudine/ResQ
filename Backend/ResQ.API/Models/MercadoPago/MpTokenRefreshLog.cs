using ResQ.API.Models.Auth;
using ResQ.API.Models.Common;

namespace ResQ.API.Models.MercadoPago;

public class MpTokenRefreshLog : BaseEntity
{
    public int MerchantId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public MerchantProfile Merchant { get; set; } = null!;
}
