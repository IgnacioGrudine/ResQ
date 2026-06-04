namespace ResQ.API.Services.MercadoPago;

public interface IMercadoPagoHttpClient
{
    Task<HttpResponseMessage> GetAsync(string path, string bearerToken, CancellationToken ct = default);
    Task<HttpResponseMessage> PostAsync(string path, HttpContent content, string? bearerToken = null, CancellationToken ct = default);
}
