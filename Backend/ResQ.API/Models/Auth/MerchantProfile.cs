using ResQ.API.Models.Catalog;
using ResQ.API.Models.Common;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Models.Orders;
using ResQ.API.Models.Reviews;

namespace ResQ.API.Models.Auth;

public class MerchantProfile : BaseEntity
{
    public int UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public MpConnectionStatus MpConnectionStatus { get; set; } = MpConnectionStatus.Disconnected;

    public User User { get; set; } = null!;
    public ICollection<MerchantCategory> MerchantCategories { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public MerchantMpCredential? MpCredential { get; set; }
    public ICollection<MpTokenRefreshLog> MpTokenRefreshLogs { get; set; } = [];
}
