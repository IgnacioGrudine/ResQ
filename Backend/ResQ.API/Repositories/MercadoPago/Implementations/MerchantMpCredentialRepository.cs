using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.MercadoPago;

/// <summary>
/// EF Core implementation of <see cref="IMerchantMpCredentialRepository"/>.
/// Manages the AES-256 encrypted Mercado Pago OAuth tokens stored per merchant,
/// and the associated token-refresh audit log.
/// </summary>
public class MerchantMpCredentialRepository(ResQDbContext db)
    : GenericRepository<MerchantMpCredential>(db), IMerchantMpCredentialRepository
{
    /// <summary>
    /// Retrieves the Mercado Pago credentials for the specified merchant.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant whose MP credentials are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="MerchantMpCredential"/> for the merchant, or <c>null</c> if the merchant
    /// has not yet connected their Mercado Pago account.
    /// </returns>
    public async Task<MerchantMpCredential?> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(c => c.MerchantId == merchantId, ct);

    /// <summary>
    /// Retrieves all active merchant credentials whose access token will expire within the
    /// specified number of days from now. Used by the background token-refresh job to
    /// proactively renew tokens before they expire.
    /// </summary>
    /// <param name="daysThreshold">
    /// The number of days ahead to look. Credentials expiring before
    /// <c>DateTime.UtcNow + daysThreshold</c> are returned.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of active <see cref="MerchantMpCredential"/> records that are expiring soon.
    /// </returns>
    public async Task<IEnumerable<MerchantMpCredential>> GetExpiringSoonAsync(
        int daysThreshold, CancellationToken ct = default)
        => await _set
            .Where(c => c.IsActive && c.AccessTokenExpiresAt < DateTime.UtcNow.AddDays(daysThreshold))
            .ToListAsync(ct);

    /// <summary>
    /// Appends a token-refresh audit entry to the <c>MpTokenRefreshLogs</c> table.
    /// Used to keep a history of every MP access token renewal performed by the system.
    /// The caller must call <c>SaveChangesAsync</c> to persist the record.
    /// </summary>
    /// <param name="log">The <see cref="MpTokenRefreshLog"/> entry to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task AddRefreshLogAsync(MpTokenRefreshLog log, CancellationToken ct = default)
        => await _db.Set<MpTokenRefreshLog>().AddAsync(log, ct);
}
