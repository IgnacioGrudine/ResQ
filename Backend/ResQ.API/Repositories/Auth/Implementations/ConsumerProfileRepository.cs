using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public class ConsumerProfileRepository(ResQDbContext db) : GenericRepository<ConsumerProfile>(db), IConsumerProfileRepository
{
    public async Task<ConsumerProfile?> GetByUserIdAsync(int userId, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(cp => cp.UserId == userId, ct);

    public async Task<ConsumerProfile?> GetByIdWithUserAsync(int profileId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(cp => cp.User)
            .FirstOrDefaultAsync(cp => cp.Id == profileId, ct);
}
