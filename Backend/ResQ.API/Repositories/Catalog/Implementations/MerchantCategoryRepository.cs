using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Catalog;

namespace ResQ.API.Repositories.Catalog;

public class MerchantCategoryRepository(ResQDbContext db) : IMerchantCategoryRepository
{
    private readonly DbSet<MerchantCategory> _set = db.Set<MerchantCategory>();

    public async Task<IEnumerable<MerchantCategory>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default)
        => await _set.Where(mc => mc.MerchantId == merchantId).ToListAsync(ct);

    public async Task AddAsync(MerchantCategory entity, CancellationToken ct = default)
        => await _set.AddAsync(entity, ct);

    public void DeleteRange(IEnumerable<MerchantCategory> entities)
        => _set.RemoveRange(entities);
}
