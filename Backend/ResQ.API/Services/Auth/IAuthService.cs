using FluentResults;
using ResQ.API.DTOs.Auth;

namespace ResQ.API.Services.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterConsumerAsync(RegisterConsumerRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> RegisterMerchantAsync(RegisterMerchantRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default);
}
