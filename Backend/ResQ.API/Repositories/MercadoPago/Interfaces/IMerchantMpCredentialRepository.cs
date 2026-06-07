using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.MercadoPago;

public interface IMerchantMpCredentialRepository : IGenericRepository<MerchantMpCredential>
{
    /// <summary>
    /// Returns the Mercado Pago OAuth credential record for the given merchant,
    /// or <c>null</c> if the merchant has not yet connected their account.
    /// </summary>
    /// <remarks>
    /// The <c>AccessToken</c> and <c>RefreshToken</c> fields in the returned entity
    /// are AES-256 encrypted at rest and must be decrypted via
    /// <see cref="IEncryptionService"/> before being sent to the Mercado Pago API.
    /// </remarks>
    /// <param name="merchantId">The identifier of the merchant profile whose credentials are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="MerchantMpCredential"/> for the merchant, or <c>null</c> if not found.
    /// </returns>
    Task<MerchantMpCredential?> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default);

    /// <summary>
    /// Returns all merchant credential records whose access token expires within the
    /// specified number of days, enabling the token-refresh job to proactively renew them.
    /// </summary>
    /// <param name="daysThreshold">
    /// The number of days from now within which a credential's expiration date must fall
    /// for it to be included in the result.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="MerchantMpCredential"/> entities expiring within the threshold;
    /// an empty collection if all tokens are still valid beyond the window.
    /// </returns>
    Task<IEnumerable<MerchantMpCredential>> GetExpiringSoonAsync(int daysThreshold, CancellationToken ct = default);

    /// <summary>
    /// Persists a new token-refresh log entry, recording the outcome of a refresh
    /// attempt for audit and debugging purposes.
    /// </summary>
    /// <param name="log">The <see cref="MpTokenRefreshLog"/> entity to insert.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddRefreshLogAsync(MpTokenRefreshLog log, CancellationToken ct = default);
}
