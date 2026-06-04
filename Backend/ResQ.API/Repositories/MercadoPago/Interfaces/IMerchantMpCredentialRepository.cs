using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.MercadoPago;

public interface IMerchantMpCredentialRepository : IGenericRepository<MerchantMpCredential>
{
    Task<MerchantMpCredential?> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default);
    Task<IEnumerable<MerchantMpCredential>> GetExpiringSoonAsync(int daysThreshold, CancellationToken ct = default);
    Task AddRefreshLogAsync(MpTokenRefreshLog log, CancellationToken ct = default);
}
