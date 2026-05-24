using ResQ.API.Models.Catalog;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Catalog;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IEnumerable<Product>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default);
    Task<Product?> GetByIdForMerchantAsync(int productId, int merchantId, CancellationToken ct = default);
    Task<IEnumerable<Product>> GetAllActiveWithMerchantAsync(CancellationToken ct = default);
    Task<Product?> GetByIdWithMerchantAsync(int productId, CancellationToken ct = default);
}
