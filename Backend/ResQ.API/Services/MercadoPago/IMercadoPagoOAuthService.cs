using FluentResults;

namespace ResQ.API.Services.MercadoPago;

public interface IMercadoPagoOAuthService
{
    string BuildAuthorizationUrl(int merchantProfileId);
    Task<Result> HandleCallbackAsync(string code, int merchantProfileId, CancellationToken ct = default);
    Task<Result> RefreshTokensAsync(int merchantProfileId, CancellationToken ct = default);
    Task<Result> DisconnectAsync(int merchantProfileId, CancellationToken ct = default);
}
