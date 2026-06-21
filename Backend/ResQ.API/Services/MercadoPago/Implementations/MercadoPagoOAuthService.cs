using System.Text.Json;
using System.Text.Json.Serialization;
using FluentResults;
using Microsoft.Extensions.Options;
using ResQ.API.Common.Errors;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Models.Settings;
using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Repositories.MercadoPago;
using ResQ.API.Services.Encryption;

namespace ResQ.API.Services.MercadoPago;

/// <summary>
/// Implements the Mercado Pago OAuth2 authorization-code flow for the ResQ marketplace model.
/// Each merchant connects their own MP account; ResQ collects a <c>marketplace_fee</c> on every payment.
/// Tokens are encrypted with AES-256 before being persisted to the database.
/// </summary>
public class MercadoPagoOAuthService(
    IOptions<MpSettings> mpOptions,
    IMercadoPagoHttpClient mpClient,
    IEncryptionService encryption,
    IMerchantMpCredentialRepository credentialRepo,
    IMerchantProfileRepository merchantRepo,
    IProductRepository productRepo) : IMercadoPagoOAuthService
{
    private readonly MpSettings _mp = mpOptions.Value;

    // ─── Build auth URL ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds the Mercado Pago authorization URL that the merchant must visit to grant
    /// ResQ access to their MP account.
    /// </summary>
    /// <remarks>
    /// The <paramref name="merchantProfileId"/> is passed as the OAuth <c>state</c> parameter so
    /// that the callback endpoint can identify which merchant completed the flow without
    /// requiring the merchant to be authenticated at callback time.
    /// </remarks>
    /// <param name="merchantProfileId">The ResQ internal profile ID of the merchant.</param>
    /// <returns>
    /// The fully constructed authorization URL pointing to <c>https://auth.mercadopago.com.ar/authorization</c>.
    /// </returns>
    public string BuildAuthorizationUrl(int merchantProfileId, string? returnOrigin = null)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"]     = _mp.ClientId,
            ["response_type"] = "code",
            ["platform_id"]   = "mp",
            ["redirect_uri"]  = _mp.RedirectUri,
            ["state"]         = MpOAuthState.Encode(merchantProfileId, returnOrigin)
        };

        var qs = string.Join("&", query.Select(kv =>
            $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

        return $"https://auth.mercadopago.com.ar/authorization?{qs}";
    }

    // ─── Handle OAuth callback ────────────────────────────────────────────────

    /// <summary>
    /// Handles the OAuth callback from Mercado Pago after the merchant approves the authorization.
    /// Exchanges the authorization code for access/refresh tokens, encrypts them, persists the
    /// credentials, and updates the merchant's connection status to <see cref="MpConnectionStatus.Connected"/>.
    /// </summary>
    /// <param name="code">The short-lived authorization code returned by Mercado Pago.</param>
    /// <param name="merchantProfileId">The ResQ merchant profile ID extracted from the OAuth <c>state</c> parameter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result.Ok"/> on success; a failed result with <c>NotFoundError</c> if the merchant
    /// does not exist, or <c>BadRequestError</c> if the code exchange fails.
    /// </returns>
    public async Task<Result> HandleCallbackAsync(
        string code, int merchantProfileId, CancellationToken ct = default)
    {
        var merchant = await merchantRepo.GetByIdAsync(merchantProfileId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Comercio no encontrado."));

        var tokenResult = await ExchangeCodeAsync(code, ct);
        if (tokenResult.IsFailed)
            return tokenResult.ToResult();

        var tokens = tokenResult.Value;
        await UpsertCredentialAsync(merchantProfileId, tokens, ct);

        merchant.MpConnectionStatus = MpConnectionStatus.Connected;
        merchant.UpdatedAt          = DateTime.UtcNow;
        merchantRepo.Update(merchant);

        await credentialRepo.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // ─── Refresh tokens (shared by HandleCallback and Hangfire job) ───────────

    /// <summary>
    /// Refreshes the access and refresh tokens for a merchant using the stored refresh token.
    /// Called both manually and by the <see cref="MpTokenRefreshJob"/> Hangfire daily job.
    /// On failure, marks the credential as inactive and sets the merchant status to
    /// <see cref="MpConnectionStatus.TokenExpired"/>.
    /// </summary>
    /// <param name="merchantProfileId">The ResQ merchant profile ID whose tokens should be refreshed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result.Ok"/> on success; a failed result with <c>NotFoundError</c> if no active
    /// credential is found, or <c>BadRequestError</c> if Mercado Pago rejects the refresh request.
    /// </returns>
    public async Task<Result> RefreshTokensAsync(
        int merchantProfileId, CancellationToken ct = default)
    {
        var credential = await credentialRepo.GetByMerchantIdAsync(merchantProfileId, ct);
        if (credential is null || !credential.IsActive)
            return Result.Fail(new NotFoundError("Credenciales MP no encontradas o inactivas."));

        var refreshToken = encryption.Decrypt(credential.RefreshToken);

        var form = new Dictionary<string, string>
        {
            ["grant_type"]    = "refresh_token",
            ["client_id"]     = _mp.ClientId,
            ["client_secret"] = _mp.ClientSecret,
            ["refresh_token"] = refreshToken
        };

        if (_mp.UseTestMode)
            form["test_token"] = "true";

        var content = new FormUrlEncodedContent(form);

        var response = await mpClient.PostAsync("/oauth/token", content, ct: ct);

        if (!response.IsSuccessStatusCode)
        {
            await MarkTokenExpiredAsync(merchantProfileId, credential, ct);
            return Result.Fail(new BadRequestError("No se pudo renovar el token de Mercado Pago."));
        }

        var json   = await response.Content.ReadAsStringAsync(ct);
        var tokens = JsonSerializer.Deserialize<MpTokenApiResponse>(json)!;

        credential.AccessToken          = encryption.Encrypt(tokens.AccessToken);
        credential.RefreshToken         = encryption.Encrypt(tokens.RefreshToken);
        credential.AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn);
        credential.UpdatedAt            = DateTime.UtcNow;
        credentialRepo.Update(credential);

        await credentialRepo.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // ─── Disconnect (HU-06) ───────────────────────────────────────────────────

    /// <summary>
    /// Disconnects the merchant from Mercado Pago by deactivating their stored credentials,
    /// setting the connection status to <see cref="MpConnectionStatus.Disconnected"/>, and
    /// deactivating all active products belonging to the merchant.
    /// </summary>
    /// <remarks>
    /// Products are deactivated because they cannot accept payments once the merchant's MP
    /// account is disconnected. The merchant must reconnect and manually reactivate their packs.
    /// </remarks>
    /// <param name="merchantProfileId">The ResQ merchant profile ID to disconnect.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="Result.Ok"/> on success; a failed result with <c>NotFoundError</c> if the
    /// merchant profile does not exist.
    /// </returns>
    public async Task<Result> DisconnectAsync(
        int merchantProfileId, CancellationToken ct = default)
    {
        var credential = await credentialRepo.GetByMerchantIdAsync(merchantProfileId, ct);
        if (credential is not null)
        {
            credential.IsActive  = false;
            credential.UpdatedAt = DateTime.UtcNow;
            credentialRepo.Update(credential);
        }

        var merchant = await merchantRepo.GetByIdAsync(merchantProfileId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Comercio no encontrado."));

        merchant.MpConnectionStatus = MpConnectionStatus.Disconnected;
        merchant.UpdatedAt          = DateTime.UtcNow;
        merchantRepo.Update(merchant);

        var products = await productRepo.GetByMerchantIdAsync(merchantProfileId, ct);
        foreach (var product in products.Where(p => p.IsActive))
        {
            product.IsActive   = false;
            product.UpdatedAt  = DateTime.UtcNow;
            productRepo.Update(product);
        }

        await credentialRepo.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Exchanges an OAuth2 authorization code for access and refresh tokens by calling
    /// <c>POST /oauth/token</c> on the Mercado Pago API.
    /// </summary>
    /// <param name="code">The short-lived authorization code received in the OAuth callback.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful result containing the deserialized <see cref="MpTokenApiResponse"/>,
    /// or a failed result with a <c>BadRequestError</c> if MP rejects the request or the
    /// response cannot be deserialized.
    /// </returns>
    private async Task<Result<MpTokenApiResponse>> ExchangeCodeAsync(
        string code, CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"]    = "authorization_code",
            ["client_id"]     = _mp.ClientId,
            ["client_secret"] = _mp.ClientSecret,
            ["code"]          = code,
            ["redirect_uri"]  = _mp.RedirectUri
        };

        // In non-production environments, request a sandbox access token. Without this flag
        // MP issues a live-mode token even when the authorizing merchant is a test user, which
        // causes Checkout Pro to reject every payment with "Unauthorized use of live credentials".
        if (_mp.UseTestMode)
            form["test_token"] = "true";

        var content = new FormUrlEncodedContent(form);

        var response = await mpClient.PostAsync("/oauth/token", content, ct: ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            return Result.Fail(new BadRequestError($"MP rechazó el intercambio de código: {error}"));
        }

        var json   = await response.Content.ReadAsStringAsync(ct);
        var tokens = JsonSerializer.Deserialize<MpTokenApiResponse>(json);

        return tokens is null
            ? Result.Fail(new BadRequestError("Respuesta inválida de Mercado Pago."))
            : Result.Ok(tokens);
    }

    /// <summary>
    /// Inserts a new <see cref="MerchantMpCredential"/> row or updates the existing one
    /// with the freshly issued tokens. Tokens are encrypted with AES-256 before persisting.
    /// </summary>
    /// <remarks>
    /// Using an upsert pattern (rather than delete + insert) preserves the primary key and
    /// audit timestamps of the existing credential record.
    /// </remarks>
    /// <param name="merchantProfileId">The ResQ merchant profile ID that owns the credentials.</param>
    /// <param name="tokens">The deserialized token response from the Mercado Pago API.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task UpsertCredentialAsync(
        int merchantProfileId, MpTokenApiResponse tokens, CancellationToken ct)
    {
        var existing = await credentialRepo.GetByMerchantIdAsync(merchantProfileId, ct);

        if (existing is null)
        {
            await credentialRepo.AddAsync(new MerchantMpCredential
            {
                MerchantId           = merchantProfileId,
                MpUserId             = tokens.UserId,
                AccessToken          = encryption.Encrypt(tokens.AccessToken),
                RefreshToken         = encryption.Encrypt(tokens.RefreshToken),
                AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn),
                Scope                = tokens.Scope,
                IsActive             = true,
                CreatedAt            = DateTime.UtcNow
            }, ct);
        }
        else
        {
            existing.MpUserId             = tokens.UserId;
            existing.AccessToken          = encryption.Encrypt(tokens.AccessToken);
            existing.RefreshToken         = encryption.Encrypt(tokens.RefreshToken);
            existing.AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn);
            existing.Scope                = tokens.Scope;
            existing.IsActive             = true;
            existing.UpdatedAt            = DateTime.UtcNow;
            credentialRepo.Update(existing);
        }
    }

    /// <summary>
    /// Marks the stored credential as inactive and updates the merchant's connection status
    /// to <see cref="MpConnectionStatus.TokenExpired"/> when a token refresh attempt fails.
    /// </summary>
    /// <param name="merchantProfileId">The ResQ merchant profile ID whose credential has expired.</param>
    /// <param name="credential">The credential entity to deactivate.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task MarkTokenExpiredAsync(
        int merchantProfileId, MerchantMpCredential credential, CancellationToken ct)
    {
        credential.IsActive  = false;
        credential.UpdatedAt = DateTime.UtcNow;
        credentialRepo.Update(credential);

        var merchant = await merchantRepo.GetByIdAsync(merchantProfileId, ct);
        if (merchant is not null)
        {
            merchant.MpConnectionStatus = MpConnectionStatus.TokenExpired;
            merchant.UpdatedAt          = DateTime.UtcNow;
            merchantRepo.Update(merchant);
        }

        await credentialRepo.SaveChangesAsync(ct);
    }

    // ─── Internal DTO for MP token API response ───────────────────────────────

    /// <summary>
    /// Internal record that maps the JSON response body returned by <c>POST /oauth/token</c>
    /// on the Mercado Pago API. Not exposed outside this service.
    /// </summary>
    private sealed record MpTokenApiResponse(
        [property: JsonPropertyName("access_token")]  string AccessToken,
        [property: JsonPropertyName("expires_in")]    int    ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("scope")]         string Scope,
        [property: JsonPropertyName("user_id")]       long   UserId);
}
