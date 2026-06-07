using System.Net.Http.Headers;

namespace ResQ.API.Services.MercadoPago;

/// <summary>
/// Typed HTTP client that wraps <c>HttpClient</c> configured against <c>https://api.mercadopago.com</c>.
/// Every request is authenticated via a per-call Bearer token so that marketplace-level calls
/// (using the admin access token) and merchant-level calls (using the merchant's own access token)
/// can coexist on the same instance.
/// </summary>
public class MercadoPagoHttpClient(HttpClient http) : IMercadoPagoHttpClient
{
    /// <summary>
    /// Sends an authenticated HTTP GET request to the Mercado Pago API.
    /// </summary>
    /// <param name="path">
    /// The relative path of the resource (e.g., <c>/v1/payments/123</c>).
    /// </param>
    /// <param name="bearerToken">
    /// The plaintext Bearer token to use for authorization. This should already be
    /// decrypted before being passed here; do not pass the encrypted DB value.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The raw <see cref="HttpResponseMessage"/> from Mercado Pago.</returns>
    public async Task<HttpResponseMessage> GetAsync(
        string path, string bearerToken, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return await http.SendAsync(request, ct);
    }

    /// <summary>
    /// Sends an HTTP POST request to the Mercado Pago API, optionally authenticated with a Bearer token.
    /// </summary>
    /// <remarks>
    /// The <paramref name="bearerToken"/> is optional to support unauthenticated token-exchange
    /// calls (e.g., <c>POST /oauth/token</c>) where the client credentials are sent in the form body.
    /// </remarks>
    /// <param name="path">
    /// The relative path of the resource (e.g., <c>/oauth/token</c> or <c>/checkout/preferences</c>).
    /// </param>
    /// <param name="content">The HTTP request body (form-urlencoded or JSON).</param>
    /// <param name="bearerToken">
    /// Optional plaintext Bearer token. When <see langword="null"/>, no <c>Authorization</c> header is added.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The raw <see cref="HttpResponseMessage"/> from Mercado Pago.</returns>
    public async Task<HttpResponseMessage> PostAsync(
        string path, HttpContent content, string? bearerToken = null, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        if (bearerToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return await http.SendAsync(request, ct);
    }
}
