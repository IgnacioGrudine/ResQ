using FluentResults;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Auth;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Settings;
using ResQ.API.Repositories.Auth;
using ResQ.API.Services.Jwt;

namespace ResQ.API.Services.Auth;

public class GoogleAuthService(
    IUserRepository users,
    IUserRoleRepository userRoles,
    IRefreshTokenRepository refreshTokens,
    IConsumerProfileRepository consumerProfiles,
    IJwtService jwtService,
    IOptions<JwtSettings> jwtOptions,
    IOptions<GoogleSettings> googleOptions) : IGoogleAuthService
{
    private readonly JwtSettings    _jwt    = jwtOptions.Value;
    private readonly GoogleSettings _google = googleOptions.Value;

    public async Task<Result<AuthResponse>> LoginWithGoogleAsync(
        string idToken, CancellationToken ct = default)
    {
        // Verify the token against Google's public keys
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_google.ClientId]
                });
        }
        catch (Exception ex) when (ex is InvalidJwtException or ArgumentException)
        {
            // ArgumentException covers null/empty idToken — Google's SDK throws that
            // before it even gets to parsing the JWT, ahead of InvalidJwtException.
            return Result.Fail(new UnauthorizedError("Token de Google inválido o expirado."));
        }

        var email    = payload.Email.ToLower().Trim();
        var googleId = payload.Subject;

        var user = await users.GetByEmailAsync(email, ct);

        if (user is null)
        {
            // New user — create consumer account automatically
            user = new User
            {
                Email     = email,
                GoogleId  = googleId,
                IsActive  = true,
                CreatedAt = DateTime.UtcNow
            };
            await users.AddAsync(user, ct);
            await users.SaveChangesAsync(ct);

            await userRoles.AddAsync(new UserRole
            {
                UserId    = user.Id,
                Role      = Role.Consumer,
                CreatedAt = DateTime.UtcNow
            }, ct);

            var firstName = payload.GivenName  ?? email.Split('@')[0];
            var lastName  = payload.FamilyName ?? string.Empty;

            var profile = new ConsumerProfile
            {
                UserId    = user.Id,
                FirstName = firstName,
                LastName  = lastName,
                CreatedAt = DateTime.UtcNow
            };
            await consumerProfiles.AddAsync(profile, ct);
            await users.SaveChangesAsync(ct);

            return Result.Ok(await IssueTokensAsync(user, Role.Consumer, profile.Id, ct));
        }

        // Existing user — link GoogleId if not yet linked
        if (!user.IsActive)
            return Result.Fail(new UnauthorizedError("La cuenta se encuentra desactivada."));

        if (user.GoogleId is null)
        {
            user.GoogleId  = googleId;
            user.UpdatedAt = DateTime.UtcNow;
            users.Update(user);
            await users.SaveChangesAsync(ct);
        }

        var existingRoles = (await userRoles.GetByUserIdAsync(user.Id, ct)).ToList();
        var primary       = existingRoles.First().Role;
        var profileId     = await ResolveProfileIdAsync(user.Id, primary, ct);

        return Result.Ok(await IssueTokensAsync(user, primary, profileId, ct));
    }

    private async Task<int?> ResolveProfileIdAsync(int userId, Role role, CancellationToken ct)
    {
        return role switch
        {
            Role.Consumer => (await consumerProfiles.GetByUserIdAsync(userId, ct))?.Id,
            _             => null
        };
    }

    private async Task<AuthResponse> IssueTokensAsync(
        User user, Role primaryRole, int? profileId, CancellationToken ct)
    {
        var roles        = new[] { primaryRole };
        var accessToken  = jwtService.GenerateAccessToken(user, roles, profileId);
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
