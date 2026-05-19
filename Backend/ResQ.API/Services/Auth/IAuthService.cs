using ResQ.API.DTOs.Auth;

namespace ResQ.API.Services.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterConsumerAsync(RegisterConsumerRequest request, CancellationToken ct = default);
    Task<AuthResponse> RegisterMerchantAsync(RegisterMerchantRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
}
