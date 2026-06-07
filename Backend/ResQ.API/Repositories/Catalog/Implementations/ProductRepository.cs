using Microsoft.EntityFrameworkCore;
using ResQ.API.Data;
using ResQ.API.Models.Catalog;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Catalog;

/// <summary>
/// EF Core implementation of <see cref="IProductRepository"/>.
/// Provides data access operations for the <see cref="Product"/> entity,
/// extending generic CRUD with catalog-specific queries used by merchant
/// management and the public consumer catalog.
/// </summary>
public class ProductRepository(ResQDbContext db) : GenericRepository<Product>(db), IProductRepository
{
    /// <summary>
    /// Retrieves all products belonging to a specific merchant, ordered so that active
    /// products appear first, then by most recently created. The query runs as no-tracking.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant whose products are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of all <see cref="Product"/> records for the merchant,
    /// sorted by activity status (active first) and then by creation date (newest first).
    /// </returns>
    public async Task<IEnumerable<Product>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Where(p => p.MerchantId == merchantId)
            .OrderByDescending(p => p.IsActive)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    /// <summary>
    /// Retrieves a single product by its identifier, scoped to a specific merchant.
    /// Used to enforce ownership before allowing a merchant to edit or delete a product.
    /// </summary>
    /// <param name="productId">The identifier of the product to retrieve.</param>
    /// <param name="merchantId">The identifier of the merchant who must own the product.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="Product"/> if it exists and belongs to the specified merchant;
    /// otherwise <c>null</c>.
    /// </returns>
    public async Task<Product?> GetByIdForMerchantAsync(int productId, int merchantId, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(p => p.Id == productId && p.MerchantId == merchantId, ct);

    /// <summary>
    /// Retrieves all active products that have available stock, eagerly loading the
    /// owning merchant together with its categories (via <c>Merchant → MerchantCategories → Category</c>)
    /// and the merchant's reviews. Used to build the public consumer catalog.
    /// The query runs as no-tracking.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="Product"/> records where <c>IsActive</c> is true and
    /// <c>StockQuantity &gt; 0</c>, with merchant, category, and review data loaded.
    /// </returns>
    public async Task<IEnumerable<Product>> GetAllActiveWithMerchantAsync(CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(p => p.Merchant)
                .ThenInclude(m => m.MerchantCategories)
                .ThenInclude(mc => mc.Category)
            .Include(p => p.Merchant)
                .ThenInclude(m => m.Reviews)
            .Where(p => p.IsActive && p.StockQuantity > 0)
            .ToListAsync(ct);

    /// <summary>
    /// Retrieves a single product by its identifier, eagerly loading the owning merchant
    /// and the merchant's reviews. Used when placing an order to obtain pricing and
    /// merchant context in a single query. The query runs as no-tracking.
    /// </summary>
    /// <param name="productId">The identifier of the product to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="Product"/> with its <c>Merchant</c> (including reviews) loaded,
    /// or <c>null</c> if no product with the given identifier exists.
    /// </returns>
    public async Task<Product?> GetByIdWithMerchantAsync(int productId, CancellationToken ct = default)
        => await _set
            .AsNoTracking()
            .Include(p => p.Merchant)
                .ThenInclude(m => m.Reviews)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);
}
