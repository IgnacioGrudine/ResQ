using ResQ.API.Models.Auth;
using ResQ.API.Models.Common;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Reviews;

namespace ResQ.API.Models.Orders;

public class Order : BaseEntity
{
    public int ConsumerId { get; set; }
    public int MerchantId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal MerchantEarnings { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public string? MpPreferenceId { get; set; }
    public long? MpPaymentId { get; set; }
    public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
    public string PickupCode { get; set; } = string.Empty;

    public ConsumerProfile Consumer { get; set; } = null!;
    public MerchantProfile Merchant { get; set; } = null!;
    public ICollection<OrderDetail> OrderDetails { get; set; } = [];
    public Review? Review { get; set; }
}
