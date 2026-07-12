using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public interface IPasswordResetTokenRepository : IGenericRepository<PasswordResetToken>
{
    /// <summary>
    /// Returns the token record matching the given hash, or <c>null</c> if no such token exists.
    /// </summary>
    /// <param name="tokenHash">The SHA-256 hash (hex-encoded) of the opaque token presented by the client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The matching <see cref="PasswordResetToken"/> with its <c>User</c> navigation property
    /// populated, or <c>null</c> if not found.
    /// </returns>
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Returns every unused, unexpired token previously issued to the given user.
    /// Used to invalidate stale tokens whenever a new reset is requested, so at most
    /// one valid token exists per user at a time.
    /// </summary>
    /// <param name="userId">The identifier of the user whose active tokens are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of the user's currently valid (unused, unexpired) tokens.</returns>
    Task<IEnumerable<PasswordResetToken>> GetActiveByUserIdAsync(int userId, CancellationToken ct = default);
}
