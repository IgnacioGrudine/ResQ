using FluentResults;
using Microsoft.Extensions.Options;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Auth;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Settings;
using ResQ.API.Repositories.Auth;
using ResQ.API.Services.Jwt;
using ResQ.API.Services.Password;

namespace ResQ.API.Services.Auth;

public class AuthService(
    IUserRepository users,
    IUserRoleRepository userRoles,
    IRefreshTokenRepository refreshTokens,
    IConsumerProfileRepository consumerProfiles,
    IMerchantProfileRepository merchantProfiles,
    IPasswordService passwordService,
    IJwtService jwtService,
    IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    // ─── Register Consumer ────────────────────────────────────────────────────

    public async Task<Result<AuthResponse>> RegisterConsumerAsync(
        RegisterConsumerRequest request, CancellationToken ct = default)
    {
        if (await users.ExistsByEmailAsync(request.Email, ct))
            return Result.Fail(new ConflictError("El email ya está registrado."));

        var user = await CreateUserAsync(request.Email, request.Password, ct);

        await userRoles.AddAsync(new UserRole
        {
            UserId    = user.Id,
            Role      = Role.Consumer,
            CreatedAt = DateTime.UtcNow
        }, ct);

        var profile = new ConsumerProfile
        {
            UserId      = user.Id,
            FirstName   = request.FirstName,
            LastName    = request.LastName,
            PhoneNumber = request.PhoneNumber,
            CreatedAt   = DateTime.UtcNow
        };
        await consumerProfiles.AddAsync(profile, ct);

        await users.SaveChangesAsync(ct);

        return Result.Ok(await IssueTokensAsync(user, Role.Consumer, profile.Id, ct));
    }

    // ─── Register Merchant ────────────────────────────────────────────────────

    public async Task<Result<AuthResponse>> RegisterMerchantAsync(
        RegisterMerchantRequest request, CancellationToken ct = default)
    {
        if (await users.ExistsByEmailAsync(request.Email, ct))
            return Result.Fail(new ConflictError("El email ya está registrado."));

        var user = await CreateUserAsync(request.Email, request.Password, ct);

        await userRoles.AddAsync(new UserRole
        {
            UserId    = user.Id,
            Role      = Role.Merchant,
            CreatedAt = DateTime.UtcNow
        }, ct);

        var profile = new MerchantProfile
        {
            UserId             = user.Id,
            BusinessName       = request.BusinessName,
            Cuit               = request.Cuit,
            Address            = request.Address,
            Latitude           = request.Latitude,
            Longitude          = request.Longitude,
            ContactPhone       = request.ContactPhone,
            MpConnectionStatus = MpConnectionStatus.Disconnected,
            CreatedAt          = DateTime.UtcNow
        };
        await merchantProfiles.AddAsync(profile, ct);

        await users.SaveChangesAsync(ct);

        return Result.Ok(await IssueTokensAsync(user, Role.Merchant, profile.Id, ct));
    }

    // ─── Login ────────────────────────────────────────────────────────────────

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByEmailAsync(request.Email, ct);
        if (user is null)
            return Result.Fail(new UnauthorizedError("Credenciales inválidas."));

        if (!passwordService.Verify(request.Password, user.PasswordHash))
            return Result.Fail(new UnauthorizedError("Credenciales inválidas."));

        if (!user.IsActive)
            return Result.Fail(new UnauthorizedError("La cuenta se encuentra desactivada."));

        var roles     = (await userRoles.GetByUserIdAsync(user.Id, ct)).ToList();
        var primary   = roles.First().Role;
        var profileId = await ResolveProfileIdAsync(user.Id, primary, ct);

        return Result.Ok(await IssueTokensAsync(user, primary, profileId, ct));
    }

    // ─── Refresh Token ────────────────────────────────────────────────────────

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await refreshTokens.GetByTokenAsync(refreshToken, ct);
        if (stored is null)
            return Result.Fail(new UnauthorizedError("Refresh token inválido."));

        if (stored.IsRevoked || stored.ExpiresAt <= DateTime.UtcNow)
            return Result.Fail(new UnauthorizedError("Refresh token expirado o revocado."));

        var user      = stored.User;
        var roles     = user.UserRoles.ToList();
        var primary   = roles.First().Role;
        var profileId = await ResolveProfileIdAsync(user.Id, primary, ct);

        var newRefreshTokenStr = jwtService.GenerateRefreshToken();

        stored.IsRevoked       = true;
        stored.ReplacedByToken = newRefreshTokenStr;
        stored.UpdatedAt       = DateTime.UtcNow;
        refreshTokens.Update(stored);

        await refreshTokens.AddAsync(new RefreshToken
        {
            UserId    = user.Id,
            Token     = newRefreshTokenStr,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        }, ct);

        await refreshTokens.SaveChangesAsync(ct);

        var accessToken = jwtService.GenerateAccessToken(user, roles.Select(r => r.Role), profileId);
        var expiresAt   = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes);

        return Result.Ok(new AuthResponse
        {
            AccessToken          = accessToken,
            RefreshToken         = newRefreshTokenStr,
            AccessTokenExpiresAt = expiresAt,
            Role                 = primary.ToString(),
            ProfileId            = profileId
        });
    }

    // ─── Logout ───────────────────────────────────────────────────────────────

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await refreshTokens.GetByTokenAsync(refreshToken, ct);
        if (stored is null) return Result.Ok();

        stored.IsRevoked = true;
        stored.UpdatedAt = DateTime.UtcNow;
        refreshTokens.Update(stored);

        await refreshTokens.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task<User> CreateUserAsync(string email, string password, CancellationToken ct)
    {
        var user = new User
        {
            Email        = email.ToLower().Trim(),
            PasswordHash = passwordService.Hash(password),
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };
        await users.AddAsync(user, ct);
        await users.SaveChangesAsync(ct);
        return user;
    }

    private async Task<int?> ResolveProfileIdAsync(int userId, Role role, CancellationToken ct)
    {
        return role switch
        {
            Role.Consumer => (await consumerProfiles.GetByUserIdAsync(userId, ct))?.Id,
            Role.Merchant => (await merchantProfiles.GetByUserIdAsync(userId, ct))?.Id,
            _             => null
        };
    }

    private async Task<AuthResponse> IssueTokensAsync(
        User user, Role primaryRole, int? profileId, CancellationToken ct)
    {
        var roles       = new[] { primaryRole };
        var accessToken = jwtService.GenerateAccessToken(user, roles, profileId);
        var refreshToken = jwtService.GenerateRefreshToken();

        await refreshTokens.AddAsync(new RefreshToken
        {
            UserId    = user.Id,
            Token     = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        }, ct);
        await refreshTokens.SaveChangesAsync(ct);

        return new AuthResponse
        {
            AccessToken          = accessToken,
            RefreshToken         = refreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes),
            Role                 = primaryRole.ToString(),
            ProfileId            = profileId
        };
    }
}
