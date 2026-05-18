using ResQ.API.Models.Auth;
using ResQ.API.Models.Common;

namespace ResQ.API.Models.MercadoPago;

public class MerchantMpCredential : BaseEntity
{
    public int MerchantId { get; set; }
    public long MpUserId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public string? Scope { get; set; }
    public bool IsActive { get; set; } = true;

    public MerchantProfile Merchant { get; set; } = null!;
}
