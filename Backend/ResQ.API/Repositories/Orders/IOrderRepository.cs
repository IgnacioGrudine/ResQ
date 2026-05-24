using ResQ.API.Models.Orders;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Orders;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<IEnumerable<Order>> GetByConsumerIdAsync(int consumerId, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default);

    /// <summary>Tracked lookup of a single order by merchant + pickup code, for status mutation.</summary>
    Task<Order?> GetByPickupCodeAsync(int merchantId, string pickupCode, CancellationToken ct = default);
}
