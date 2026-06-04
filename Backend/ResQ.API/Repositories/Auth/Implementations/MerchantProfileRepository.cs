using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public class MerchantProfileRepository(ResQDbContext db) : GenericRepository<MerchantProfile>(db), IMerchantProfileRepository
{
    public async Task<MerchantProfile?> GetByUserIdAsync(int userId, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(mp => mp.UserId == userId, ct);

    public async Task<IEnumerable<MerchantProfile>> GetAllWithCatalogDataAsync(CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(m => m.MerchantCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Products.Where(p => p.IsActive && p.StockQuantity > 0))
            .Include(m => m.Reviews)
            .Where(m => m.Products.Any(p => p.IsActive && p.StockQuantity > 0))
            .ToListAsync(ct);

    public async Task<MerchantProfile?> GetByIdWithPublicDetailAsync(int merchantId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(m => m.MerchantCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Products.Where(p => p.IsActive && p.StockQuantity > 0))
            .Include(m => m.Reviews)
            .FirstOrDefaultAsync(m => m.Id == merchantId, ct);

    public async Task<MerchantProfile?> GetByIdWithCategoriesAsync(int merchantId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(m => m.MerchantCategories).ThenInclude(mc => mc.Category)
            .FirstOrDefaultAsync(m => m.Id == merchantId, ct);
}
