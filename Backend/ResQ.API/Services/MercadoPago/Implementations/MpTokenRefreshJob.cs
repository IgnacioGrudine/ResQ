using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.MercadoPago;

namespace ResQ.API.Services.MercadoPago;

/// <summary>
/// Hangfire recurring job that proactively refreshes Mercado Pago OAuth tokens before they expire.
/// Scheduled to run once per day. Tokens expiring within <see cref="DaysThreshold"/> days are
/// refreshed so that merchants never experience payment failures due to expired credentials.
/// A <see cref="MpTokenRefreshLog"/> entry is written for every attempt regardless of outcome,
/// providing an audit trail for operational monitoring.
/// </summary>
public class MpTokenRefreshJob(
    IMerchantMpCredentialRepository credentialRepo,
    IMercadoPagoOAuthService oauthService) : IMpTokenRefreshJob
{
    /// <summary>
    /// Number of days before expiry at which a token is considered "expiring soon"
    /// and will be proactively refreshed by this job.
    /// </summary>
    private const int DaysThreshold = 7;

    /// <summary>
    /// Queries all active credentials expiring within <see cref="DaysThreshold"/> days,
    /// attempts to refresh each one via <see cref="IMercadoPagoOAuthService.RefreshTokensAsync"/>,
    /// and records the outcome in <c>MpTokenRefreshLogs</c>.
    /// </summary>
    /// <remarks>
    /// If the expiring list is empty no database write is performed.
    /// Each credential is processed independently so a single failure does not abort the entire batch.
    /// </remarks>
    public async Task ExecuteAsync()
    {
        var expiring = (await credentialRepo.GetExpiringSoonAsync(DaysThreshold)).ToList();

        foreach (var credential in expiring)
        {
            var result = await oauthService.RefreshTokensAsync(credential.MerchantId);

            await credentialRepo.AddRefreshLogAsync(new MpTokenRefreshLog
            {
                MerchantId   = credential.MerchantId,
                Success      = result.IsSuccess,
                ErrorMessage = result.IsFailed ? result.Errors[0].Message : null,
                CreatedAt    = DateTime.UtcNow
            });
        }

        if (expiring.Count > 0)
            await credentialRepo.SaveChangesAsync();
    }
}
