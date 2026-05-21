using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Catalog;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Catalog;

public class ProductRepository(ResQDbContext db) : GenericRepository<Product>(db), IProductRepository
{
    public async Task<IEnumerable<Product>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Where(p => p.MerchantId == merchantId)
            .OrderByDescending(p => p.IsActive)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<Product?> GetByIdForMerchantAsync(int productId, int merchantId, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(p => p.Id == productId && p.MerchantId == merchantId, ct);
}
