using ResQ.API.Models.Catalog;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Catalog;

public interface ICategoryRepository : IGenericRepository<Category>
{
    /// <summary>
    /// Returns all categories whose primary keys are contained in the provided
    /// set of identifiers.
    /// </summary>
    /// <remarks>
    /// Used when assigning categories to a merchant or product to validate that all
    /// supplied identifiers correspond to existing categories in a single query.
    /// </remarks>
    /// <param name="ids">The collection of category identifiers to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="Category"/> entities matching the supplied identifiers.
    /// Identifiers that do not correspond to any category are silently omitted from the result.
    /// </returns>
    Task<IEnumerable<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> if any merchant is currently assigned to the specified category.
    /// Used as a guard before deletion to prevent orphaning merchant profiles.
    /// </summary>
    Task<bool> IsInUseAsync(int id, CancellationToken ct = default);
}
