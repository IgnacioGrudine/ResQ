using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public interface IUserRoleRepository : IGenericRepository<UserRole>
{
    Task<IEnumerable<UserRole>> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int userId, Role role, CancellationToken ct = default);
}
