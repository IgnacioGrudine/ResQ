using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

/// <summary>
/// EF Core implementation of <see cref="IRefreshTokenRepository"/>.
/// Provides data access for the <see cref="RefreshToken"/> entity, extending generic CRUD
/// with a query that loads the token together with the owning user and their roles,
/// as required for the token-refresh flow.
/// </summary>
public class RefreshTokenRepository(ResQDbContext db) : GenericRepository<RefreshToken>(db), IRefreshTokenRepository
{
    /// <summary>
    /// Retrieves a refresh token by its opaque string value, eagerly loading the associated
    /// <c>User</c> and the user's <c>UserRoles</c> navigation properties.
    /// This data is required to validate the token and issue a new JWT without
    /// an additional round-trip to the database.
    /// </summary>
    /// <param name="token">The opaque refresh token string to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="RefreshToken"/> with <c>User</c> and <c>UserRoles</c> loaded,
    /// or <c>null</c> if no matching token is found.
    /// </returns>
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => await _set
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
            .FirstOrDefaultAsync(rt => rt.Token == token, ct);

    /// <summary>
    /// Retrieves every non-revoked, unexpired refresh token for the given user, tracked
    /// so the caller can revoke them in place.
    /// </summary>
    /// <param name="userId">The identifier of the user whose active tokens are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of the user's currently active refresh tokens.</returns>
    public async Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(int userId, CancellationToken ct = default)
        => await _set
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);
}
