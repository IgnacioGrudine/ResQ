using ResQ.API.Models.Catalog;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Catalog;

public interface IProductRepository : IGenericRepository<Product>
{
    /// <summary>
    /// Returns all products (active and inactive) that belong to the given merchant.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant profile whose products are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="Product"/> entities owned by the specified merchant;
    /// an empty collection if the merchant has no products.
    /// </returns>
    Task<IEnumerable<Product>> GetByMerchantIdAsync(int merchantId, CancellationToken ct = default);

    /// <summary>
    /// Returns a single product by its identifier, validating that it belongs to the
    /// specified merchant to prevent cross-tenant access.
    /// </summary>
    /// <param name="productId">The identifier of the product to retrieve.</param>
    /// <param name="merchantId">The identifier of the merchant profile that must own the product.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The matching <see cref="Product"/> if it exists and belongs to the merchant,
    /// or <c>null</c> otherwise.
    /// </returns>
    Task<Product?> GetByIdForMerchantAsync(int productId, int merchantId, CancellationToken ct = default);

    /// <summary>
    /// Returns all active products across all merchants, eagerly loading the owning
    /// <see cref="MerchantProfile"/> and its geographic coordinates for catalog and
    /// geolocation queries.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of active <see cref="Product"/> entities with the <c>Merchant</c>
    /// navigation property populated.
    /// </returns>
    Task<IEnumerable<Product>> GetAllActiveWithMerchantAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a single product by its identifier, eagerly loading the owning
    /// <see cref="MerchantProfile"/> navigation property.
    /// </summary>
    /// <param name="productId">The identifier of the product to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="Product"/> with the <c>Merchant</c> navigation property populated,
    /// or <c>null</c> if not found.
    /// </returns>
    Task<Product?> GetByIdWithMerchantAsync(int productId, CancellationToken ct = default);
}
