using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    /// <summary>
    /// Returns the refresh token record matching the given opaque token string,
    /// or <c>null</c> if no such token exists in the database.
    /// </summary>
    /// <remarks>
    /// Used during the token-refresh and logout flows to validate the provided token
    /// and retrieve its associated user information before rotating or revoking it.
    /// </remarks>
    /// <param name="token">The opaque refresh token string to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The matching <see cref="RefreshToken"/> entity with its <c>User</c> navigation
    /// property populated, or <c>null</c> if not found.
    /// </returns>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
}
