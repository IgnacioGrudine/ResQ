using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Orders;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Orders;

public class OrderRepository(ResQDbContext db) : GenericRepository<Order>(db), IOrderRepository
{
    public async Task<IEnumerable<Order>> GetByConsumerIdAsync(int consumerId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(o => o.Merchant)
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .Where(o => o.ConsumerId == consumerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<Order>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(o => o.Consumer)
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .Where(o => o.MerchantId == merchantId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<Order?> GetByPickupCodeAsync(int merchantId, string pickupCode, CancellationToken ct = default)
        => await _set
            .Include(o => o.Consumer)
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.MerchantId == merchantId && o.PickupCode == pickupCode, ct);
}
