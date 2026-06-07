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
    /// <param name="ids">The collection of category identifiers to fetch.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="Category"/> objects matching the supplied identifiers.
    /// Identifiers that do not exist in the database are silently omitted from the result.
    /// </returns>
    public async Task<IEnumerable<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
        => await _set.Where(c => ids.Contains(c.Id)).ToListAsync(ct);
}
