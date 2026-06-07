using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Catalog;

namespace ResQ.API.Repositories.Catalog;

/// <summary>
/// EF Core implementation of <see cref="IMerchantCategoryRepository"/>.
/// Manages the many-to-many join table between merchants and food categories.
/// Does not extend <c>GenericRepository</c> because the composite-key entity
/// does not derive from <c>BaseEntity</c>.
/// </summary>
public class MerchantCategoryRepository(ResQDbContext db) : IMerchantCategoryRepository
{
    private readonly DbSet<MerchantCategory> _set = db.Set<MerchantCategory>();

    /// <summary>
    /// Retrieves all category assignments for the specified merchant.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant whose category links are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="MerchantCategory"/> join records for the merchant.
    /// Returns an empty collection if the merchant has no category assignments.
    /// </returns>
    public async Task<IEnumerable<MerchantCategory>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default)
        => await _set.Where(mc => mc.MerchantId == merchantId).ToListAsync(ct);

    /// <summary>
    /// Adds a new merchant–category association to the change tracker.
    /// The caller is responsible for calling <c>SaveChangesAsync</c> to persist the record.
    /// </summary>
    /// <param name="entity">The <see cref="MerchantCategory"/> join record to add.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task AddAsync(MerchantCategory entity, CancellationToken ct = default)
        => await _set.AddAsync(entity, ct);

    /// <summary>
    /// Marks a collection of merchant–category associations for deletion in the change tracker.
    /// The caller is responsible for calling <c>SaveChangesAsync</c> to persist the removal.
    /// </summary>
    /// <param name="entities">The <see cref="MerchantCategory"/> join records to remove.</param>
    public void DeleteRange(IEnumerable<MerchantCategory> entities)
        => _set.RemoveRange(entities);
}
