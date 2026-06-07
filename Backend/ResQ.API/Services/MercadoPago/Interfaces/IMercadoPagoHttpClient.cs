namespace ResQ.API.Services.MercadoPago;

public interface IMercadoPagoHttpClient
{
    /// <summary>
    /// Sends an authenticated HTTP GET request to the Mercado Pago API at the specified path.
    /// </summary>
    /// <param name="path">
    /// The relative path of the Mercado Pago API endpoint, e.g. <c>/v1/payments/{id}</c>.
    /// </param>
    /// <param name="bearerToken">
    /// The merchant's decrypted Mercado Pago access token used as the Bearer credential
    /// in the <c>Authorization</c> header.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The raw <see cref="HttpResponseMessage"/> returned by the Mercado Pago API.</returns>
    Task<HttpResponseMessage> GetAsync(string path, string bearerToken, CancellationToken ct = default);

    /// <summary>
    /// Sends an HTTP POST request to the Mercado Pago API at the specified path,
    /// optionally including a Bearer token for authenticated endpoints.
    /// </summary>
    /// <param name="path">
    /// The relative path of the Mercado Pago API endpoint, e.g. <c>/oauth/token</c>.
    /// </param>
    /// <param name="content">The serialized HTTP request body to send.</param>
    /// <param name="bearerToken">
    /// The optional Bearer token. Pass <c>null</c> for unauthenticated endpoints such as
    /// the initial OAuth token exchange.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The raw <see cref="HttpResponseMessage"/> returned by the Mercado Pago API.</returns>
    Task<HttpResponseMessage> PostAsync(string path, HttpContent content, string? bearerToken = null, CancellationToken ct = default);
}
