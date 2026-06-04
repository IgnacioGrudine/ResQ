using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public interface IMerchantProfileRepository : IGenericRepository<MerchantProfile>
{
    Task<MerchantProfile?> GetByUserIdAsync(int userId, CancellationToken ct = default);
    Task<IEnumerable<MerchantProfile>> GetAllWithCatalogDataAsync(CancellationToken ct = default);
    Task<MerchantProfile?> GetByIdWithPublicDetailAsync(int merchantId, CancellationToken ct = default);
    Task<MerchantProfile?> GetByIdWithCategoriesAsync(int merchantId, CancellationToken ct = default);
}
