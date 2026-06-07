namespace ResQ.API.Services.MercadoPago;

public interface IMpTokenRefreshJob
{
    /// <summary>
    /// Iterates over all merchant Mercado Pago credentials that are expiring within
    /// the configured threshold window and proactively refreshes their access tokens
    /// before they become invalid.
    /// </summary>
    /// <remarks>
    /// Intended to be invoked by a background scheduler (e.g., a hosted service or
    /// a Hangfire/Quartz job) on a recurring schedule. Each refresh is performed via
    /// <see cref="IMercadoPagoOAuthService.RefreshTokensAsync"/> and the result is
    /// logged to <c>MpTokenRefreshLogs</c>.
    /// </remarks>
    /// <returns>A task that represents the asynchronous execution of the refresh job.</returns>
    Task ExecuteAsync();
}
