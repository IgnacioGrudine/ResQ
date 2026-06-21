using FluentResults;

namespace ResQ.API.Services.MercadoPago;

public interface IMercadoPagoOAuthService
{
    /// <summary>
    /// Constructs the Mercado Pago OAuth 2.0 authorization URL to which the merchant
    /// must be redirected in order to grant ResQ access to their Mercado Pago account.
    /// </summary>
    /// <remarks>
    /// The URL includes the application's client ID, the required scopes, and a
    /// <c>state</c> parameter that encodes <paramref name="merchantProfileId"/> together
    /// with <paramref name="returnOrigin"/> so the callback can both correlate the response
    /// with the correct merchant and return them to the frontend origin they started from.
    /// </remarks>
    /// <param name="merchantProfileId">The identifier of the merchant profile initiating the OAuth flow.</param>
    /// <param name="returnOrigin">
    /// The frontend origin (scheme + host + port) the merchant initiated the flow from, used to
    /// build the post-callback redirect so the merchant lands back where their session cookie lives.
    /// Optional; when omitted the callback falls back to the configured frontend base URL.
    /// </param>
    /// <returns>The fully-qualified Mercado Pago authorization URL as a string.</returns>
    string BuildAuthorizationUrl(int merchantProfileId, string? returnOrigin = null);

    /// <summary>
    /// Handles the OAuth 2.0 authorization callback by exchanging the temporary
    /// authorization code for an access token and refresh token, then persisting them
    /// encrypted to the merchant's credential record.
    /// </summary>
    /// <remarks>
    /// Tokens are encrypted with AES-256 via <see cref="IEncryptionService"/> before
    /// being stored. This method is idempotent: if credentials already exist for the
    /// merchant they are replaced.
    /// </remarks>
    /// <param name="code">The short-lived authorization code received from Mercado Pago's redirect.</param>
    /// <param name="merchantProfileId">The identifier of the merchant profile completing the OAuth flow.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result"/> if the token exchange and persistence succeeded;
    /// or a failed result if the code is invalid, the exchange request failed, or a
    /// database error occurred.
    /// </returns>
    Task<Result> HandleCallbackAsync(string code, int merchantProfileId, CancellationToken ct = default);

    /// <summary>
    /// Uses the stored encrypted refresh token to obtain a new access token from
    /// Mercado Pago and updates the credential record with the refreshed tokens.
    /// </summary>
    /// <remarks>
    /// Called by the background token-refresh job before the access token expires.
    /// On success the new tokens are re-encrypted and persisted; the expiration
    /// timestamp is updated accordingly.
    /// </remarks>
    /// <param name="merchantProfileId">The identifier of the merchant profile whose tokens are to be refreshed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result"/> if the token was refreshed and persisted;
    /// or a failed result if no credentials exist for the merchant, the refresh
    /// request failed, or a database error occurred.
    /// </returns>
    Task<Result> RefreshTokensAsync(int merchantProfileId, CancellationToken ct = default);

    /// <summary>
    /// Revokes the merchant's Mercado Pago OAuth credentials and removes the
    /// stored credential record, effectively disconnecting their account from ResQ.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile to disconnect.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result"/> if the credentials were revoked and deleted;
    /// or a failed result if no credentials exist or the revocation request failed.
    /// </returns>
    Task<Result> DisconnectAsync(int merchantProfileId, CancellationToken ct = default);
}
