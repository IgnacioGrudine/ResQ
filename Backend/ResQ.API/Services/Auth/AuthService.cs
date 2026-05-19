using FluentResults;
using Microsoft.Extensions.Options;
using ResQ.API.Common.Errors;
using ResQ.API.Data.UnitOfWork;
using ResQ.API.DTOs.Auth;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Settings;
using ResQ.API.Services.Jwt;
using ResQ.API.Services.Password;

namespace ResQ.API.Services.Auth;

public class AuthService(
    IUnitOfWork uow,
    IPasswordService passwordService,
    IJwtService jwtService,
    IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    // ─── Register Consumer ────────────────────────────────────────────────────

    public async Task<Result<AuthResponse>> RegisterConsumerAsync(
        RegisterConsumerRequest request, CancellationToken ct = default)
    {
        if (await uow.Users.ExistsByEmailAsync(request.Email, ct))
            return Result.Fail(new ConflictError("El email ya está registrado."));

        var user = await CreateUserAsync(request.Email, request.Password, ct);

        var userRole = new UserRole
        {
            UserId    = user.Id,
            Role      = Role.Consumer,
            CreatedAt = DateTime.UtcNow
        };
        await uow.UserRoles.AddAsync(userRole, ct);

        var profile = new ConsumerProfile
        {
            UserId      = user.Id,
            FirstName   = request.FirstName,
            LastName    = request.LastName,
            PhoneNumber = request.PhoneNumber,
            CreatedAt   = DateTime.UtcNow
        };
        await uow.ConsumerProfiles.AddAsync(profile, ct);

        await uow.SaveChangesAsync(ct); // UserRole + ConsumerProfile en una sola transacción

        return Result.Ok(await IssueTokensAsync(user, Role.Consumer, profile.Id, ct));
    }

    // ─── Register Merchant ────────────────────────────────────────────────────

    public async Task<Result<AuthResponse>> RegisterMerchantAsync(
        RegisterMerchantRequest request, CancellationToken ct = default)
    {
        if (await uow.Users.ExistsByEmailAsync(request.Email, ct))
            return Result.Fail(new ConflictError("El email ya está registrado."));

        var user = await CreateUserAsync(request.Email, request.Password, ct);

        var userRole = new UserRole
        {
            UserId    = user.Id,
            Role      = Role.Merchant,
            CreatedAt = DateTime.UtcNow
        };
        await uow.UserRoles.AddAsync(userRole, ct);

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
        await uow.MerchantProfiles.AddAsync(profile, ct);

        await uow.SaveChangesAsync(ct); // UserRole + MerchantProfile en una sola transacción

        return Result.Ok(await IssueTokensAsync(user, Role.Merchant, profile.Id, ct));
    }

    // ─── Login ────────────────────────────────────────────────────────────────

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await uow.Users.GetByEmailAsync(request.Email, ct);
        if (user is null)
            return Result.Fail(new UnauthorizedError("Credenciales inválidas."));

        if (!passwordService.Verify(request.Password, user.PasswordHash))
            return Result.Fail(new UnauthorizedError("Credenciales inválidas."));

        if (!user.IsActive)
            return Result.Fail(new UnauthorizedError("La cuenta se encuentra desactivada."));

        var roles     = (await uow.UserRoles.GetByUserIdAsync(user.Id, ct)).ToList();
        var primary   = roles.First().Role;
        var profileId = await ResolveProfileIdAsync(user.Id, primary, ct);

        return Result.Ok(await IssueTokensAsync(user, primary, profileId, ct));
    }

    // ─── Refresh Token ────────────────────────────────────────────────────────

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await uow.RefreshTokens.GetByTokenAsync(refreshToken, ct);
        if (stored is null)
            return Result.Fail(new UnauthorizedError("Refresh token inválido."));

        if (stored.IsRevoked || stored.ExpiresAt <= DateTime.UtcNow)
            return Result.Fail(new UnauthorizedError("Refresh token expirado o revocado."));

        var user    = stored.User;
        var roles   = user.UserRoles.ToList();
        var primary = roles.First().Role;
        var profileId = await ResolveProfileIdAsync(user.Id, primary, ct);

        var newRefreshTokenStr = jwtService.GenerateRefreshToken();

        // Revocar token viejo con trazabilidad
        stored.IsRevoked       = true;
        stored.ReplacedByToken = newRefreshTokenStr;
        stored.UpdatedAt       = DateTime.UtcNow;
        uow.RefreshTokens.Update(stored);

        // Emitir nuevo refresh token
        var newRefreshToken = new RefreshToken
        {
            UserId    = user.Id,
            Token     = newRefreshTokenStr,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };
        await uow.RefreshTokens.AddAsync(newRefreshToken, ct);
        await uow.SaveChangesAsync(ct);

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
        var stored = await uow.RefreshTokens.GetByTokenAsync(refreshToken, ct);
        if (stored is null) return Result.Ok(); // idempotente

        stored.IsRevoked = true;
        stored.UpdatedAt = DateTime.UtcNow;
        uow.RefreshTokens.Update(stored);

        await uow.SaveChangesAsync(ct);
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
        await uow.Users.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct); // necesitamos el user.Id antes de crear los hijos
        return user;
    }

    private async Task<int?> ResolveProfileIdAsync(int userId, Role role, CancellationToken ct)
    {
        return role switch
        {
            Role.Consumer => (await uow.ConsumerProfiles.GetByUserIdAsync(userId, ct))?.Id,
            Role.Merchant => (await uow.MerchantProfiles.GetByUserIdAsync(userId, ct))?.Id,
            _             => null
        };
    }

    private async Task<AuthResponse> IssueTokensAsync(
        User user, Role primaryRole, int? profileId, CancellationToken ct)
    {
        var roles        = new[] { primaryRole };
        var accessToken  = jwtService.GenerateAccessToken(user, roles, profileId);
        var refreshToken = jwtService.GenerateRefreshToken();
        var expiresAt    = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes);

        var refreshTokenEntity = new RefreshToken
        {
            UserId    = user.Id,
            Token     = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };
        await uow.RefreshTokens.AddAsync(refreshTokenEntity, ct);
        await uow.SaveChangesAsync(ct);

        return new AuthResponse
        {
            AccessToken          = accessToken,
            RefreshToken         = refreshToken,
            AccessTokenExpiresAt = expiresAt,
            Role                 = primaryRole.ToString(),
            ProfileId            = profileId
        };
    }
}
