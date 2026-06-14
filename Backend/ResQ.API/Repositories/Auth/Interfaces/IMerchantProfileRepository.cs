using ResQ.API.Models.Auth;
using ResQ.API.Repositories.Common;

namespace ResQ.API.Repositories.Auth;

public interface IMerchantProfileRepository : IGenericRepository<MerchantProfile>
{
    /// <summary>
    /// Returns the merchant profile associated with the given user account,
    /// or <c>null</c> if no profile has been created for that user.
    /// </summary>
    /// <param name="userId">The identifier of the <see cref="User"/> whose merchant profile is requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="MerchantProfile"/>, or <c>null</c> if not found.</returns>
    Task<MerchantProfile?> GetByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> if a merchant profile with the given CUIT already exists.
    /// Used during registration to detect duplicates before hitting the DB unique constraint.
    /// </summary>
    /// <param name="cuit">The CUIT string to check (format XX-XXXXXXXX-X).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> ExistsByCuitAsync(string cuit, CancellationToken ct = default);

    /// <summary>
    /// Returns all merchant profiles that have at least one active product, eagerly loading
    /// the product and category data required to render the public catalog.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A collection of <see cref="MerchantProfile"/> entities with their associated
    /// catalog data populated.
    /// </returns>
    Task<IEnumerable<MerchantProfile>> GetAllWithCatalogDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a merchant profile by its identifier, eagerly loading the public-facing
    /// navigation properties such as active products, categories, and reviews.
    /// </summary>
    /// <remarks>
    /// Intended for the public merchant detail endpoint. Does not load private data
    /// such as Mercado Pago credentials.
    /// </remarks>
    /// <param name="merchantId">The identifier of the merchant profile to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="MerchantProfile"/> with public detail navigation properties populated,
    /// or <c>null</c> if not found.
    /// </returns>
    Task<MerchantProfile?> GetByIdWithPublicDetailAsync(int merchantId, CancellationToken ct = default);

    /// <summary>
    /// Returns a merchant profile by its identifier, eagerly loading the assigned
    /// categories collection.
    /// </summary>
    /// <remarks>
    /// Used when updating a merchant's categories so the current set can be compared
    /// and replaced efficiently.
    /// </remarks>
    /// <param name="merchantId">The identifier of the merchant profile to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="MerchantProfile"/> with the <c>MerchantCategories</c> collection
    /// populated, or <c>null</c> if not found.
    /// </returns>
    Task<MerchantProfile?> GetByIdWithCategoriesAsync(int merchantId, CancellationToken ct = default);
}
