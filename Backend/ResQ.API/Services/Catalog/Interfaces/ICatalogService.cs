using FluentResults;
using ResQ.API.DTOs.Catalog;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Shared;

namespace ResQ.API.Services.Catalog;

public interface ICatalogService
{
    /// <summary>
    /// Returns all available food categories that can be used to classify merchant products
    /// and filter the public catalog.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing the full list of <see cref="CategoryResponse"/>
    /// items; or a failed result on unexpected errors.
    /// </returns>
    Task<Result<IEnumerable<CategoryResponse>>> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the list of all merchants that currently have at least one active product
    /// available for purchase, suitable for displaying the public merchant catalog.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a collection of <see cref="MerchantListItemResponse"/>
    /// summary items; or a failed result on unexpected errors.
    /// </returns>
    Task<Result<IEnumerable<MerchantListItemResponse>>> GetCatalogAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the public detail view of a specific merchant, including their active
    /// products, categories, ratings, and contact information.
    /// </summary>
    /// <param name="merchantId">The identifier of the merchant profile to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a <see cref="MerchantDetailResponse"/>;
    /// or a failed result if no merchant with the given identifier exists.
    /// </returns>
    Task<Result<MerchantDetailResponse>> GetMerchantDetailAsync(int merchantId, CancellationToken ct = default);

    /// <summary>
    /// Returns a filtered and optionally geo-sorted list of available surprise packs
    /// from all active merchants.
    /// </summary>
    /// <param name="lat">
    /// Optional latitude of the consumer's current position, used to compute distances
    /// and sort results by proximity when combined with <paramref name="lon"/>.
    /// </param>
    /// <param name="lon">
    /// Optional longitude of the consumer's current position, used to compute distances
    /// and sort results by proximity when combined with <paramref name="lat"/>.
    /// </param>
    /// <param name="search">Optional free-text search term matched against pack and merchant names.</param>
    /// <param name="categoryId">Optional category filter; when supplied, only packs belonging to that category are returned.</param>
    /// <param name="maxPrice">Optional upper price bound in ARS; packs with a higher price are excluded.</param>
    /// <param name="maxDistance">Optional maximum distance in kilometres from the consumer's position; requires both <paramref name="lat"/> and <paramref name="lon"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a collection of <see cref="PackListItemResponse"/>
    /// items matching the supplied filters; or a failed result on unexpected errors.
    /// </returns>
    Task<Result<IEnumerable<PackListItemResponse>>> GetPacksAsync(
        double? lat, double? lon,
        string? search, int? categoryId,
        decimal? maxPrice, double? maxDistance,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the public details of a single surprise pack by its identifier.
    /// </summary>
    /// <param name="packId">The identifier of the product (pack) to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a <see cref="PackListItemResponse"/>;
    /// or a failed result if no pack with the given identifier exists or the pack is inactive.
    /// </returns>
    Task<Result<PackListItemResponse>> GetPackByIdAsync(int packId, CancellationToken ct = default);

    // ── Admin category management ──────────────────────────────────────────────

    /// <summary>Creates a new food category and returns the persisted record.</summary>
    Task<Result<CategoryResponse>> CreateCategoryAsync(string name, CancellationToken ct = default);

    /// <summary>Renames an existing category. Returns 404 if the category does not exist.</summary>
    Task<Result<CategoryResponse>> UpdateCategoryAsync(int id, string name, CancellationToken ct = default);

    /// <summary>
    /// Deletes a category. Returns 409 Conflict if one or more merchants are still assigned to it.
    /// </summary>
    Task<Result> DeleteCategoryAsync(int id, CancellationToken ct = default);
}
