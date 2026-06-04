using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public class UserRepository(ResQDbContext db) : GenericRepository<User>(db), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);

    public async Task<User?> GetWithRolesAsync(int id, CancellationToken ct = default)
        => await _set
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await _set.AnyAsync(u => u.Email == email.ToLower(), ct);
}
