using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

/// <summary>
/// EF Core implementation of <see cref="IPasswordResetTokenRepository"/>.
/// </summary>
public class PasswordResetTokenRepository(ResQDbContext db)
    : GenericRepository<PasswordResetToken>(db), IPasswordResetTokenRepository
{
    /// <summary>
    /// Retrieves a token by its hash, eagerly loading the owning <c>User</c> so the
    /// caller can update the password without an additional round-trip.
    /// </summary>
    /// <param name="tokenHash">The SHA-256 hash (hex-encoded) to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="PasswordResetToken"/> with <c>User</c> loaded, or <c>null</c>.</returns>
    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => await _set
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    /// <summary>
    /// Retrieves every unused, unexpired token for the given user.
    /// </summary>
    /// <param name="userId">The identifier of the user whose active tokens are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of the user's currently valid tokens.</returns>
    public async Task<IEnumerable<PasswordResetToken>> GetActiveByUserIdAsync(int userId, CancellationToken ct = default)
        => await _set
            .Where(t => t.UserId == userId && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);
}
