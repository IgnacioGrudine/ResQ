using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public interface IMerchantProfileRepository : IGenericRepository<MerchantProfile>
{
    Task<MerchantProfile?> GetByUserIdAsync(int userId, CancellationToken ct = default);
}
