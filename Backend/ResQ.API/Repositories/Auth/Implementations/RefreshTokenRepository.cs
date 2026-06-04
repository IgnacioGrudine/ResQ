using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public class RefreshTokenRepository(ResQDbContext db) : GenericRepository<RefreshToken>(db), IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => await _set
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
            .FirstOrDefaultAsync(rt => rt.Token == token, ct);
}
