using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public class MerchantProfileRepository(ResQDbContext db) : GenericRepository<MerchantProfile>(db), IMerchantProfileRepository
{
    public async Task<MerchantProfile?> GetByUserIdAsync(int userId, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(mp => mp.UserId == userId, ct);
}
