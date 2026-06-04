using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Catalog;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Catalog;

public class CategoryRepository(ResQDbContext db) : GenericRepository<Category>(db), ICategoryRepository
{
    public async Task<IEnumerable<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
        => await _set.Where(c => ids.Contains(c.Id)).ToListAsync(ct);
}
