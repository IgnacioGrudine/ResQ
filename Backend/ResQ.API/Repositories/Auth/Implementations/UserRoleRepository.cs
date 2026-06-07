using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

/// <summary>
/// EF Core implementation of <see cref="IUserRoleRepository"/>.
/// Provides data access operations for the <see cref="UserRole"/> join entity,
/// supporting role assignment queries and existence checks used during
/// authentication and authorization flows.
/// </summary>
public class UserRoleRepository(ResQDbContext db) : GenericRepository<UserRole>(db), IUserRoleRepository
{
    /// <summary>
    /// Retrieves all role assignments for the specified user.
    /// </summary>
    /// <param name="userId">The identifier of the user whose roles are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="UserRole"/> records for the user.
    /// Returns an empty collection if the user has no assigned roles.
    /// </returns>
    public async Task<IEnumerable<UserRole>> GetByUserIdAsync(int userId, CancellationToken ct = default)
        => await _set.Where(ur => ur.UserId == userId).ToListAsync(ct);

    /// <summary>
    /// Checks whether a specific role has already been assigned to the given user.
    /// </summary>
    /// <param name="userId">The identifier of the user to check.</param>
    /// <param name="role">The role to look for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the user already has the specified role assigned; otherwise <c>false</c>.
    /// </returns>
    public async Task<bool> ExistsAsync(int userId, Role role, CancellationToken ct = default)
        => await _set.AnyAsync(ur => ur.UserId == userId && ur.Role == role, ct);
}
