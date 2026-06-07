using ResQ.API.Models.Catalog;

namespace ResQ.API.Repositories.Catalog;

public interface IMerchantCategoryRepository
{
    /// <summary>
    /// Returns all category assignments for the given merchant.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant profile whose category assignments are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="MerchantCategory"/> join entities for the specified merchant;
    /// an empty collection if the merchant has no categories assigned.
    /// </returns>
    Task<IEnumerable<MerchantCategory>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new merchant-to-category assignment to the change tracker so it will
    /// be inserted on the next call to <c>SaveChangesAsync</c>.
    /// </summary>
    /// <param name="entity">The <see cref="MerchantCategory"/> join entity to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(MerchantCategory entity, CancellationToken ct = default);

    /// <summary>
    /// Marks a collection of merchant-to-category assignments for removal in the change
    /// tracker so they will be deleted on the next call to <c>SaveChangesAsync</c>.
    /// </summary>
    /// <param name="entities">The <see cref="MerchantCategory"/> join entities to delete.</param>
    void DeleteRange(IEnumerable<MerchantCategory> entities);
}
