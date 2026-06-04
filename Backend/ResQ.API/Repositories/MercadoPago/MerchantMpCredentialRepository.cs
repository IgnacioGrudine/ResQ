using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.MercadoPago;

public class MerchantMpCredentialRepository(ResQDbContext db)
    : GenericRepository<MerchantMpCredential>(db), IMerchantMpCredentialRepository
{
    public async Task<MerchantMpCredential?> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(c => c.MerchantId == merchantId, ct);

    public async Task<IEnumerable<MerchantMpCredential>> GetExpiringSoonAsync(
        int daysThreshold, CancellationToken ct = default)
        => await _set
            .Where(c => c.IsActive && c.AccessTokenExpiresAt < DateTime.UtcNow.AddDays(daysThreshold))
            .ToListAsync(ct);
}
