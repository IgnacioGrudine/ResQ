using FluentResults;
using ResQ.API.DTOs.Auth;

namespace ResQ.API.Services.Auth;

public interface IGoogleAuthService
{
    Task<Result<AuthResponse>> LoginWithGoogleAsync(string idToken, CancellationToken ct = default);
}
