using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Catalog;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Catalog;

/// <summary>
/// EF Core implementation of <see cref="ICategoryRepository"/>.
/// Provides data access operations for the <see cref="Category"/> entity,
/// extending generic CRUD with a batch-lookup query used when validating
/// category assignments for merchants.
/// </summary>
public class CategoryRepository(ResQDbContext db) : GenericRepository<Category>(db), ICategoryRepository
{
    /// <summary>
    /// Retrieves all categories whose identifiers are contained in the provided set.
    /// Used to validate that a list of category IDs submitted by a merchant all correspond
    /// to existing records before persisting the assignment.
    /// </summary>
    public async Task<IEnumerable<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
        => await _set.Where(c => ids.Contains(c.Id)).ToListAsync(ct);

    /// <summary>
    /// Returns <c>true</c> if at least one <see cref="MerchantCategory"/> record references this category.
    /// Used as a pre-delete guard to prevent removing categories that still have active merchant assignments.
    /// </summary>
    public async Task<bool> IsInUseAsync(int id, CancellationToken ct = default)
        => await _db.Set<MerchantCategory>().AnyAsync(mc => mc.CategoryId == id, ct);
}
