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

/// <summary>
/// Handles all authentication operations: consumer and merchant registration, login,
/// JWT access token issuance, opaque refresh token rotation, and logout.
/// </summary>
/// <remarks>
/// Uses a token rotation strategy: every call to <see cref="RefreshTokenAsync"/> revokes
/// the incoming refresh token and issues a brand-new one, leaving a traceable chain via
/// <c>ReplacedByToken</c>. Business errors are surfaced as <see cref="Result"/> failures
/// rather than exceptions, following the FluentResults pattern used throughout ResQ.
/// </remarks>
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

    /// <summary>
    /// Registers a new consumer account, assigns the <see cref="Role.Consumer"/> role,
    /// creates the associated <see cref="ConsumerProfile"/>, and immediately issues
    /// an access/refresh token pair.
    /// </summary>
    /// <param name="request">Registration payload containing email, password, and personal data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{AuthResponse}"/> with tokens and profile metadata,
    /// or a <c>ConflictError</c> if the email is already registered.
    /// </returns>
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

    /// <summary>
    /// Registers a new merchant account, assigns the <see cref="Role.Merchant"/> role,
    /// creates the associated <see cref="MerchantProfile"/> with Mercado Pago connection
    /// status defaulted to <see cref="MpConnectionStatus.Disconnected"/>, and immediately
    /// issues an access/refresh token pair.
    /// </summary>
    /// <param name="request">Registration payload containing email, password, business details, and geolocation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{AuthResponse}"/> with tokens and profile metadata,
    /// or a <c>ConflictError</c> if the email is already registered.
    /// </returns>
    public async Task<Result<AuthResponse>> RegisterMerchantAsync(
        RegisterMerchantRequest request, CancellationToken ct = default)
    {
        if (await users.ExistsByEmailAsync(request.Email, ct))
            return Result.Fail(new ConflictError("El email ya está registrado."));

        if (await merchantProfiles.ExistsByCuitAsync(request.Cuit, ct))
            return Result.Fail(new ConflictError("El CUIT ingresado ya está registrado en la plataforma."));

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

    /// <summary>
    /// Authenticates a user with email and password, then issues a fresh access/refresh token pair.
    /// </summary>
    /// <param name="request">Login payload containing the user's email and plain-text password.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{AuthResponse}"/> with tokens and the user's primary role,
    /// or an <c>UnauthorizedError</c> if credentials are invalid or the account is inactive.
    /// </returns>
    /// <remarks>
    /// To avoid user enumeration, both "email not found" and "wrong password" cases return
    /// the same generic error message ("Credenciales inválidas.").
    /// </remarks>
    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByEmailAsync(request.Email, ct);
        if (user is null)
            return Result.Fail(new UnauthorizedError("Credenciales inválidas."));

        if (user.PasswordHash is null)
            return Result.Fail(new UnauthorizedError("Esta cuenta usa Google para iniciar sesión."));

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

    /// <summary>
    /// Rotates the refresh token: revokes the supplied token, records its replacement chain,
    /// persists a new refresh token, and returns a fresh access/refresh token pair.
    /// </summary>
    /// <param name="refreshToken">The opaque refresh token string previously issued to the client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{AuthResponse}"/> with the new token pair,
    /// or an <c>UnauthorizedError</c> if the token is unknown, already revoked, or expired.
    /// </returns>
    /// <remarks>
    /// The rotation strategy ensures each refresh token can only be used once.
    /// The old token is marked as revoked and its <c>ReplacedByToken</c> field links to
    /// the newly issued token, providing a full audit trail.
    /// </remarks>
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

    /// <summary>
    /// Revokes the given refresh token, effectively ending the user's session.
    /// </summary>
    /// <param name="refreshToken">The opaque refresh token string to revoke.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Always returns a successful <see cref="Result"/>. If the token does not exist
    /// (e.g., already expired and purged), the operation is treated as a no-op.
    /// </returns>
    /// <remarks>
    /// The idempotent design means clients can safely call logout even if the token
    /// has already been revoked or has never existed.
    /// </remarks>
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

    /// <summary>
    /// Creates and persists a new <see cref="User"/> entity with a BCrypt-hashed password.
    /// </summary>
    /// <param name="email">The user's email address. Normalized to lowercase and trimmed before storage.</param>
    /// <param name="password">The plain-text password to hash.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created and persisted <see cref="User"/> entity.</returns>
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

    /// <summary>
    /// Resolves the profile ID for a given user and role by querying the corresponding
    /// profile repository (<see cref="ConsumerProfile"/> or <see cref="MerchantProfile"/>).
    /// </summary>
    /// <param name="userId">The internal user ID.</param>
    /// <param name="role">The primary role that determines which profile table to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The profile ID if a matching profile exists; <c>null</c> for unrecognized roles.
    /// </returns>
    private async Task<int?> ResolveProfileIdAsync(int userId, Role role, CancellationToken ct)
    {
        return role switch
        {
            Role.Consumer => (await consumerProfiles.GetByUserIdAsync(userId, ct))?.Id,
            Role.Merchant => (await merchantProfiles.GetByUserIdAsync(userId, ct))?.Id,
            _             => null
        };
    }

    /// <summary>
    /// Generates a new access token and refresh token pair, persists the refresh token,
    /// and constructs the <see cref="AuthResponse"/> returned to the caller.
    /// </summary>
    /// <param name="user">The authenticated user entity.</param>
    /// <param name="primaryRole">The user's primary role to embed in the JWT claims.</param>
    /// <param name="profileId">The profile ID to embed in the JWT <c>profileId</c> claim.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="AuthResponse"/> containing both tokens and their metadata.</returns>
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
