using System.Net.Http.Headers;

namespace ResQ.API.Services.MercadoPago;

public class MercadoPagoHttpClient(HttpClient http) : IMercadoPagoHttpClient
{
    public async Task<HttpResponseMessage> GetAsync(
        string path, string bearerToken, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return await http.SendAsync(request, ct);
    }

    public async Task<HttpResponseMessage> PostAsync(
        string path, HttpContent content, string? bearerToken = null, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        if (bearerToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return await http.SendAsync(request, ct);
    }
}
