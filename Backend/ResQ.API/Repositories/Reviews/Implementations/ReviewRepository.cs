using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Reviews;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Reviews;

public class ReviewRepository(ResQDbContext db) : GenericRepository<Review>(db), IReviewRepository
{
    public async Task<IEnumerable<Review>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Where(r => r.MerchantId == merchantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
}
