using ResQ.API.Models.Catalog;

namespace ResQ.API.Repositories.Catalog;

public interface IMerchantCategoryRepository
{
    Task<IEnumerable<MerchantCategory>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default);
    Task AddAsync(MerchantCategory entity, CancellationToken ct = default);
    void DeleteRange(IEnumerable<MerchantCategory> entities);
}
