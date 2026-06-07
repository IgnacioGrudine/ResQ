using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public interface IUserRoleRepository : IGenericRepository<UserRole>
{
    /// <summary>
    /// Returns all role assignments for the given user.
    /// </summary>
    /// <param name="userId">The identifier of the user whose role assignments are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="UserRole"/> entities for the specified user;
    /// an empty collection if the user has no roles assigned.
    /// </returns>
    Task<IEnumerable<UserRole>> GetByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Checks whether the given user already has the specified role assigned.
    /// </summary>
    /// <param name="userId">The identifier of the user to check.</param>
    /// <param name="role">The role to check for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if a <see cref="UserRole"/> record exists for the given user and role;
    /// <c>false</c> otherwise.
    /// </returns>
    Task<bool> ExistsAsync(int userId, Role role, CancellationToken ct = default);
}
