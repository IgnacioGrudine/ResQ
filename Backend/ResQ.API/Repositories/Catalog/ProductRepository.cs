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

    public async Task<IEnumerable<Product>> GetAllActiveWithMerchantAsync(CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(p => p.Merchant)
                .ThenInclude(m => m.MerchantCategories)
                .ThenInclude(mc => mc.Category)
            .Include(p => p.Merchant)
                .ThenInclude(m => m.Reviews)
            .Where(p => p.IsActive && p.StockQuantity > 0)
            .ToListAsync(ct);

    public async Task<Product?> GetByIdWithMerchantAsync(int productId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(p => p.Merchant)
                .ThenInclude(m => m.Reviews)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);
}
