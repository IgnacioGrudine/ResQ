using FluentResults;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Catalog;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Shared;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Services.Merchants;

namespace ResQ.API.Services.Catalog;

public class CatalogService(
    IMerchantService merchantService,
    IProductRepository productRepository) : ICatalogService
{
    public Task<Result<IEnumerable<MerchantListItemResponse>>> GetCatalogAsync(CancellationToken ct = default)
        => merchantService.GetCatalogAsync(ct);

    public Task<Result<MerchantDetailResponse>> GetMerchantDetailAsync(int merchantId, CancellationToken ct = default)
        => merchantService.GetByIdAsync(merchantId, ct);

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
            return MapPackBase<PackListItemResponse>(p, p.Merchant, distanceKm);
        });

        if (maxDistance.HasValue && lat.HasValue && lon.HasValue)
            responses = responses.Where(r => r.DistanceKm <= maxDistance.Value);

        var sorted = lat.HasValue && lon.HasValue
            ? responses.OrderBy(r => r.DistanceKm)
            : responses.OrderBy(r => r.SalePrice);

        return Result.Ok(sorted.AsEnumerable());
    }

    public async Task<Result<PackDetailResponse>> GetPackDetailAsync(int packId, CancellationToken ct = default)
    {
        var product = await productRepository.GetByIdWithMerchantAsync(packId, ct);
        if (product is null)
            return Result.Fail(new NotFoundError($"Pack {packId} not found."));

        var merchant = product.Merchant;

        var otherPacks = merchant.Products
            .Select(p => MapPackBase<PackListItemResponse>(p, merchant))
            .ToList();

        var recentReviews = merchant.Reviews
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new DTOs.Reviews.ReviewResponse
            {
                Id        = r.Id,
                Rating    = r.Rating,
                Comment   = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList();

        var response = MapPackBase<PackDetailResponse>(product, merchant);
        response.MerchantPhone          = merchant.ContactPhone;
        response.MerchantCategoriesFull = merchant.MerchantCategories
            .Select(mc => new CategoryResponse { Id = mc.CategoryId, Name = mc.Category.Name })
            .ToList();
        response.MerchantOtherPacks    = otherPacks;
        response.MerchantRecentReviews = recentReviews;

        return Result.Ok(response);
    }

    private static T MapPackBase<T>(Models.Catalog.Product p, Models.Auth.MerchantProfile merchant, double? distanceKm = null)
        where T : PackListItemResponse, new() => new()
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
        MerchantId            = merchant.Id,
        MerchantName          = merchant.BusinessName,
        MerchantAddress       = merchant.Address,
        MerchantLatitude      = merchant.Latitude,
        MerchantLongitude     = merchant.Longitude,
        MerchantCategories    = merchant.MerchantCategories.Select(mc => mc.Category.Name).ToList(),
        MerchantAverageRating = merchant.Reviews.Count > 0
            ? Math.Round((decimal)merchant.Reviews.Average(r => r.Rating), 1) : 0,
        MerchantReviewCount   = merchant.Reviews.Count,
        DistanceKm            = distanceKm
    };

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
