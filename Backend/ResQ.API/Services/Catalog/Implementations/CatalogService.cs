using FluentResults;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Catalog;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Shared;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Services.Merchants;

namespace ResQ.API.Services.Catalog;

/// <summary>
/// Provides the public-facing catalog of merchants and surprise food packs for consumers.
/// All operations are read-only and do not require authentication.
/// </summary>
/// <remarks>
/// Filtering and distance computation are performed in memory after an initial database fetch
/// because geospatial calculations (Haversine) are not directly translated by EF Core / Npgsql.
/// When location is provided, results are sorted by ascending distance; otherwise by ascending price.
/// </remarks>
public class CatalogService(
    IMerchantService merchantService,
    IProductRepository productRepository,
    ICategoryRepository categoryRepository) : ICatalogService
{
    /// <summary>
    /// Returns all food categories available in the platform.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing an enumerable of <see cref="CategoryResponse"/>.
    /// </returns>
    public async Task<Result<IEnumerable<CategoryResponse>>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var cats = await categoryRepository.GetAllAsync(ct);
        return Result.Ok(cats.Select(c => new CategoryResponse { Id = c.Id, Name = c.Name }));
    }

    /// <summary>
    /// Returns a flat list of all merchants that have active products, suitable for a catalog overview page.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing an enumerable of <see cref="MerchantListItemResponse"/>.
    /// Delegates to <see cref="IMerchantService.GetCatalogAsync"/>.
    /// </returns>
    public Task<Result<IEnumerable<MerchantListItemResponse>>> GetCatalogAsync(CancellationToken ct = default)
        => merchantService.GetCatalogAsync(ct);

    /// <summary>
    /// Returns the public detail of a single merchant, including their active packs and recent reviews.
    /// </summary>
    /// <param name="merchantId">The merchant profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{MerchantDetailResponse}"/>, or a <c>NotFoundError</c>
    /// if no merchant with the given ID exists.
    /// </returns>
    public Task<Result<MerchantDetailResponse>> GetMerchantDetailAsync(int merchantId, CancellationToken ct = default)
        => merchantService.GetByIdAsync(merchantId, ct);

    /// <summary>
    /// Returns active surprise packs filtered and optionally sorted by the consumer's location.
    /// </summary>
    /// <param name="lat">Consumer's latitude in decimal degrees. Used for distance calculation and sorting.</param>
    /// <param name="lon">Consumer's longitude in decimal degrees. Used for distance calculation and sorting.</param>
    /// <param name="search">Free-text search applied to pack name and merchant business name (case-insensitive).</param>
    /// <param name="categoryId">Filters packs to merchants that belong to the specified category.</param>
    /// <param name="maxPrice">Upper bound for the pack's sale price (inclusive).</param>
    /// <param name="maxDistance">Maximum distance in kilometres from the consumer's location. Requires <paramref name="lat"/> and <paramref name="lon"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> with the filtered and sorted enumerable of <see cref="PackListItemResponse"/>.
    /// Distance is included in each item when coordinates are provided; otherwise it is <c>null</c>.
    /// </returns>
    /// <remarks>
    /// Filtering is applied sequentially: text search, then category, then price cap.
    /// The distance filter is applied after projection so that it operates on the already-computed
    /// <c>DistanceKm</c> value. Sorting is proximity-first when coordinates are available,
    /// price-first otherwise.
    /// </remarks>
    public async Task<Result<IEnumerable<PackListItemResponse>>> GetPacksAsync(
        double? lat, double? lon,
        string? search, int? categoryId,
        decimal? maxPrice, double? maxDistance,
        CancellationToken ct = default)
    {
        var products = await productRepository.GetAllActiveWithMerchantAsync(ct);

        var filtered = products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Merchant.BusinessName.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (categoryId.HasValue)
            filtered = filtered.Where(p =>
                p.Merchant.MerchantCategories.Any(mc => mc.CategoryId == categoryId.Value));

        if (maxPrice.HasValue)
            filtered = filtered.Where(p => p.SalePrice <= maxPrice.Value);

        var responses = filtered.Select(p =>
        {
            double? distanceKm = lat.HasValue && lon.HasValue
                ? Math.Round(HaversineKm(lat.Value, lon.Value, (double)p.Merchant.Latitude, (double)p.Merchant.Longitude), 1)
                : null;
            return MapPack(p, distanceKm);
        });

        if (maxDistance.HasValue && lat.HasValue && lon.HasValue)
            responses = responses.Where(r => r.DistanceKm <= maxDistance.Value);

        var sorted = lat.HasValue && lon.HasValue
            ? responses.OrderBy(r => r.DistanceKm)
            : responses.OrderBy(r => r.SalePrice);

        return Result.Ok(sorted.AsEnumerable());
    }

    /// <summary>
    /// Returns the public detail of a single surprise pack by its ID.
    /// </summary>
    /// <param name="packId">The product (pack) ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{PackListItemResponse}"/>, or a <c>NotFoundError</c>
    /// if no active pack with the given ID exists.
    /// </returns>
    public async Task<Result<PackListItemResponse>> GetPackByIdAsync(int packId, CancellationToken ct = default)
    {
        var product = await productRepository.GetByIdWithMerchantAsync(packId, ct);
        if (product is null)
            return Result.Fail(new NotFoundError($"Pack {packId} not found."));

        return Result.Ok(MapPack(product));
    }

    /// <summary>
    /// Maps a <see cref="Models.Catalog.Product"/> entity to a <see cref="PackListItemResponse"/> DTO,
    /// optionally including the pre-computed distance from the consumer.
    /// </summary>
    /// <param name="p">The product entity with its related merchant data eagerly loaded.</param>
    /// <param name="distanceKm">
    /// Distance in kilometres from the consumer to the merchant, rounded to one decimal place.
    /// Pass <c>null</c> when the consumer's location is not available.
    /// </param>
    /// <returns>A populated <see cref="PackListItemResponse"/> DTO.</returns>
    private static PackListItemResponse MapPack(Models.Catalog.Product p, double? distanceKm = null) => new()
    {
        Id                    = p.Id,
        Name                  = p.Name,
        Description           = p.Description,
        ImageUrl              = p.ImageUrl,
        ProductType           = p.ProductType.ToString(),
        OriginalPrice         = p.OriginalPrice,
        SalePrice             = p.SalePrice,
        StockQuantity         = p.StockQuantity,
        PickupTimeStart       = p.PickupTimeStart,
        PickupTimeEnd         = p.PickupTimeEnd,
        MerchantId            = p.Merchant.Id,
        MerchantName          = p.Merchant.BusinessName,
        MerchantAverageRating = p.Merchant.Reviews.Count > 0
            ? Math.Round((decimal)p.Merchant.Reviews.Average(r => r.Rating), 1) : 0,
        MerchantReviewCount   = p.Merchant.Reviews.Count,
        DistanceKm            = distanceKm
    };

    /// <summary>
    /// Calculates the great-circle distance in kilometres between two geographic coordinates
    /// using the Haversine formula.
    /// </summary>
    /// <param name="lat1">Latitude of the first point in decimal degrees.</param>
    /// <param name="lon1">Longitude of the first point in decimal degrees.</param>
    /// <param name="lat2">Latitude of the second point in decimal degrees.</param>
    /// <param name="lon2">Longitude of the second point in decimal degrees.</param>
    /// <returns>The distance between the two points in kilometres.</returns>
    /// <remarks>
    /// Uses Earth's mean radius of 6 371 km. The result is an approximation; it does not
    /// account for Earth's ellipsoidal shape, which is acceptable for the short urban
    /// distances typical in the ResQ platform.
    /// </remarks>
    // ── Admin category management ─────────────────────────────────────────────

    public async Task<Result<CategoryResponse>> CreateCategoryAsync(string name, CancellationToken ct = default)
    {
        var category = new Models.Catalog.Category { Name = name.Trim() };
        await categoryRepository.AddAsync(category, ct);
        await categoryRepository.SaveChangesAsync(ct);
        return Result.Ok(new CategoryResponse { Id = category.Id, Name = category.Name });
    }

    public async Task<Result<CategoryResponse>> UpdateCategoryAsync(int id, string name, CancellationToken ct = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, ct);
        if (category is null)
            return Result.Fail(new NotFoundError($"Category {id} not found."));

        category.Name = name.Trim();
        categoryRepository.Update(category);
        await categoryRepository.SaveChangesAsync(ct);
        return Result.Ok(new CategoryResponse { Id = category.Id, Name = category.Name });
    }

    public async Task<Result> DeleteCategoryAsync(int id, CancellationToken ct = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, ct);
        if (category is null)
            return Result.Fail(new NotFoundError($"Category {id} not found."));

        if (await categoryRepository.IsInUseAsync(id, ct))
            return Result.Fail(new ConflictError(
                "La categoría está asignada a uno o más comercios. Reasignalos antes de eliminarla."));

        categoryRepository.Delete(category);
        await categoryRepository.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
