using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public class UserRoleRepository(ResQDbContext db) : GenericRepository<UserRole>(db), IUserRoleRepository
{
    public async Task<IEnumerable<UserRole>> GetByUserIdAsync(int userId, CancellationToken ct = default)
        => await _set.Where(ur => ur.UserId == userId).ToListAsync(ct);

    public async Task<bool> ExistsAsync(int userId, Role role, CancellationToken ct = default)
        => await _set.AnyAsync(ur => ur.UserId == userId && ur.Role == role, ct);
}
