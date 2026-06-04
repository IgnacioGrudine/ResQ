using ResQ.API.Models.Catalog;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Catalog;

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<IEnumerable<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
}
