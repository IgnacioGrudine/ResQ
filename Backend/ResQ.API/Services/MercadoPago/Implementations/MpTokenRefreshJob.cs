using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.MercadoPago;

namespace ResQ.API.Services.MercadoPago;

public class MpTokenRefreshJob(
    IMerchantMpCredentialRepository credentialRepo,
    IMercadoPagoOAuthService oauthService) : IMpTokenRefreshJob
{
    private const int DaysThreshold = 7;

    public async Task ExecuteAsync()
    {
        var expiring = (await credentialRepo.GetExpiringSoonAsync(DaysThreshold)).ToList();

        foreach (var credential in expiring)
        {
            var result = await oauthService.RefreshTokensAsync(credential.MerchantId);

            await credentialRepo.AddRefreshLogAsync(new MpTokenRefreshLog
            {
                MerchantId   = credential.MerchantId,
                Success      = result.IsSuccess,
                ErrorMessage = result.IsFailed ? result.Errors[0].Message : null,
                CreatedAt    = DateTime.UtcNow
            });
        }

        if (expiring.Count > 0)
            await credentialRepo.SaveChangesAsync();
    }
}
