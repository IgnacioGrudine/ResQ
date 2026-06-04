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

    public string BuildAuthorizationUrl(int merchantProfileId)
    {
        var query = new Dictionary<string, string>
        {
            ["client_id"]     = _mp.ClientId,
            ["response_type"] = "code",
            ["platform_id"]   = "mp",
            ["redirect_uri"]  = _mp.RedirectUri,
            ["state"]         = merchantProfileId.ToString()
        };

        var qs = string.Join("&", query.Select(kv =>
            $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

        return $"https://auth.mercadopago.com.ar/authorization?{qs}";
    }

    // ─── Handle OAuth callback ────────────────────────────────────────────────

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

    public async Task<Result> RefreshTokensAsync(
        int merchantProfileId, CancellationToken ct = default)
    {
        var credential = await credentialRepo.GetByMerchantIdAsync(merchantProfileId, ct);
        if (credential is null || !credential.IsActive)
            return Result.Fail(new NotFoundError("Credenciales MP no encontradas o inactivas."));

        var refreshToken = encryption.Decrypt(credential.RefreshToken);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "refresh_token",
            ["client_id"]     = _mp.ClientId,
            ["client_secret"] = _mp.ClientSecret,
            ["refresh_token"] = refreshToken
        });

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

    private async Task<Result<MpTokenApiResponse>> ExchangeCodeAsync(
        string code, CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "authorization_code",
            ["client_id"]     = _mp.ClientId,
            ["client_secret"] = _mp.ClientSecret,
            ["code"]          = code,
            ["redirect_uri"]  = _mp.RedirectUri
        });

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

    private sealed record MpTokenApiResponse(
        [property: JsonPropertyName("access_token")]  string AccessToken,
        [property: JsonPropertyName("expires_in")]    int    ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string RefreshToken,
        [property: JsonPropertyName("scope")]         string Scope,
        [property: JsonPropertyName("user_id")]       long   UserId);
}
