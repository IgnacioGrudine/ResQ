using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

/// <summary>
/// EF Core implementation of <see cref="IMerchantProfileRepository"/>.
/// Provides data access operations for the <see cref="MerchantProfile"/> entity,
/// extending generic CRUD with merchant-specific queries that include catalog,
/// category, and review data needed by various API endpoints.
/// </summary>
public class MerchantProfileRepository(ResQDbContext db) : GenericRepository<MerchantProfile>(db), IMerchantProfileRepository
{
    /// <summary>
    /// Retrieves the merchant profile associated with the given user account identifier.
    /// </summary>
    /// <param name="userId">The identifier of the <c>User</c> whose merchant profile is requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The matching <see cref="MerchantProfile"/>, or <c>null</c> if no profile exists
    /// for the specified user.
    /// </returns>
    public async Task<MerchantProfile?> GetByUserIdAsync(int userId, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(mp => mp.UserId == userId, ct);

    public async Task<bool> ExistsByCuitAsync(string cuit, CancellationToken ct = default)
        => await _set.AnyAsync(mp => mp.Cuit == cuit, ct);

    /// <summary>
    /// Retrieves all merchant profiles that have at least one active product with available stock,
    /// eagerly loading categories (via <c>MerchantCategories → Category</c>), active products
    /// (filtered inline to <c>IsActive &amp;&amp; StockQuantity &gt; 0</c>), and reviews.
    /// The query runs as no-tracking for read-only scenarios.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="MerchantProfile"/> objects with catalog data loaded,
    /// restricted to merchants that currently have purchasable packs.
    /// </returns>
    public async Task<IEnumerable<MerchantProfile>> GetAllWithCatalogDataAsync(CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(m => m.MerchantCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Products.Where(p => p.IsActive && p.StockQuantity > 0))
            .Include(m => m.Reviews)
            .Where(m => m.Products.Any(p => p.IsActive && p.StockQuantity > 0))
            .ToListAsync(ct);

    /// <summary>
    /// Retrieves the public-facing detail for a single merchant, eagerly loading categories
    /// (via <c>MerchantCategories → Category</c>), active in-stock products, and all reviews.
    /// The query runs as no-tracking for read-only scenarios.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="MerchantProfile"/> with public detail data loaded, or <c>null</c>
    /// if no merchant with the given identifier exists.
    /// </returns>
    public async Task<MerchantProfile?> GetByIdWithPublicDetailAsync(int merchantId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(m => m.MerchantCategories).ThenInclude(mc => mc.Category)
            .Include(m => m.Products.Where(p => p.IsActive && p.StockQuantity > 0))
            .Include(m => m.Reviews)
            .FirstOrDefaultAsync(m => m.Id == merchantId, ct);

    /// <summary>
    /// Retrieves a merchant profile by identifier with its associated categories loaded
    /// (via <c>MerchantCategories → Category</c>). Used when only category data is needed,
    /// without loading products or reviews. The query runs as no-tracking.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="MerchantProfile"/> with categories loaded, or <c>null</c>
    /// if no merchant with the given identifier exists.
    /// </returns>
    public async Task<MerchantProfile?> GetByIdWithCategoriesAsync(int merchantId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(m => m.MerchantCategories).ThenInclude(mc => mc.Category)
            .FirstOrDefaultAsync(m => m.Id == merchantId, ct);
}
