using FluentResults;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Products;
using ResQ.API.DTOs.Reviews;
using ResQ.API.DTOs.Shared;
using ResQ.API.Models.Catalog;
using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Repositories.Reviews;
using ResQ.API.Services.Orders;

namespace ResQ.API.Services.Merchants;

public class MerchantService(
    IMerchantProfileRepository merchantProfiles,
    IMerchantCategoryRepository merchantCategories,
    IReviewRepository reviews,
    IOrderService orderService) : IMerchantService
{
    // ─── Public catalog ───────────────────────────────────────────────────────

    public async Task<Result<IEnumerable<MerchantListItemResponse>>> GetCatalogAsync(CancellationToken ct = default)
    {
        var merchants = await merchantProfiles.GetAllWithCatalogDataAsync(ct);

        var result = merchants.Select(m => new MerchantListItemResponse
        {
            Id                 = m.Id,
            BusinessName       = m.BusinessName,
            Address            = m.Address,
            Latitude           = m.Latitude,
            Longitude          = m.Longitude,
            ContactPhone       = m.ContactPhone,
            Categories         = m.MerchantCategories.Select(mc => mc.Category.Name).ToList(),
            AverageRating      = m.Reviews.Any() ? Math.Round((decimal)m.Reviews.Average(r => r.Rating), 1) : 0,
            ReviewCount        = m.Reviews.Count,
            MinSalePrice       = m.Products.Any() ? m.Products.Min(p => p.SalePrice) : 0,
            ActiveProductCount = m.Products.Count
        });

        return Result.Ok(result);
    }

    // ─── Public detail ────────────────────────────────────────────────────────

    public async Task<Result<MerchantDetailResponse>> GetByIdAsync(int merchantId, CancellationToken ct = default)
    {
        var merchant = await merchantProfiles.GetByIdWithPublicDetailAsync(merchantId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Comercio no encontrado."));

        return Result.Ok(new MerchantDetailResponse
        {
            Id             = merchant.Id,
            BusinessName   = merchant.BusinessName,
            Address        = merchant.Address,
            Latitude       = merchant.Latitude,
            Longitude      = merchant.Longitude,
            ContactPhone   = merchant.ContactPhone,
            Categories     = merchant.MerchantCategories
                                 .Select(mc => new CategoryResponse { Id = mc.CategoryId, Name = mc.Category.Name })
                                 .ToList(),
            AverageRating  = merchant.Reviews.Any() ? Math.Round((decimal)merchant.Reviews.Average(r => r.Rating), 1) : 0,
            ReviewCount    = merchant.Reviews.Count,
            ActiveProducts = merchant.Products.Select(MapProduct).ToList(),
            RecentReviews  = merchant.Reviews
                                 .OrderByDescending(r => r.CreatedAt)
                                 .Take(10)
                                 .Select(r => new ReviewResponse
                                 {
                                     Id        = r.Id,
                                     Rating    = r.Rating,
                                     Comment   = r.Comment,
                                     CreatedAt = r.CreatedAt
                                 })
                                 .ToList()
        });
    }

    // ─── Authenticated merchant — own profile ─────────────────────────────────

    public async Task<Result<MerchantProfileResponse>> GetMyProfileAsync(int merchantProfileId, CancellationToken ct = default)
    {
        var merchant = await merchantProfiles.GetByIdWithCategoriesAsync(merchantProfileId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Perfil de comercio no encontrado."));

        return Result.Ok(MapMerchantProfile(merchant));
    }

    public async Task<Result<MerchantProfileResponse>> UpdateMyProfileAsync(
        int merchantProfileId, UpdateMerchantProfileRequest request, CancellationToken ct = default)
    {
        var merchant = await merchantProfiles.GetByIdAsync(merchantProfileId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Perfil de comercio no encontrado."));

        merchant.BusinessName = request.BusinessName;
        merchant.Address      = request.Address;
        merchant.Latitude     = request.Latitude;
        merchant.Longitude    = request.Longitude;
        merchant.ContactPhone = request.ContactPhone;
        merchant.UpdatedAt    = DateTime.UtcNow;
        merchantProfiles.Update(merchant);

        if (request.CategoryIds.Count > 0)
        {
            var existing = await merchantCategories.GetByMerchantIdAsync(merchantProfileId, ct);
            merchantCategories.DeleteRange(existing);

            foreach (var catId in request.CategoryIds.Distinct())
                await merchantCategories.AddAsync(
                    new MerchantCategory { MerchantId = merchantProfileId, CategoryId = catId }, ct);
        }

        await merchantProfiles.SaveChangesAsync(ct);

        var updated = await merchantProfiles.GetByIdWithCategoriesAsync(merchantProfileId, ct);
        return Result.Ok(MapMerchantProfile(updated!));
    }

    // ─── Authenticated merchant — orders ─────────────────────────────────────

    public Task<Result<IEnumerable<MerchantOrderSummaryResponse>>> GetMyOrdersAsync(int merchantProfileId, CancellationToken ct = default)
        => orderService.GetMerchantOrdersAsync(merchantProfileId, ct);

    // ─── Authenticated merchant — reviews ────────────────────────────────────

    public async Task<Result<IEnumerable<ReviewResponse>>> GetMyReviewsAsync(int merchantProfileId, CancellationToken ct = default)
    {
        var result = await reviews.GetByMerchantIdAsync(merchantProfileId, ct);

        return Result.Ok(result.Select(r => new ReviewResponse
        {
            Id        = r.Id,
            Rating    = r.Rating,
            Comment   = r.Comment,
            CreatedAt = r.CreatedAt
        }));
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private static ProductResponse MapProduct(Models.Catalog.Product p) => new()
    {
        Id              = p.Id,
        Name            = p.Name,
        Description     = p.Description,
        ImageUrl        = p.ImageUrl,
        ProductType     = p.ProductType.ToString(),
        OriginalPrice   = p.OriginalPrice,
        SalePrice       = p.SalePrice,
        StockQuantity   = p.StockQuantity,
        PickupTimeStart = p.PickupTimeStart,
        PickupTimeEnd   = p.PickupTimeEnd,
        IsActive        = p.IsActive
    };

    private static MerchantProfileResponse MapMerchantProfile(Models.Auth.MerchantProfile m) => new()
    {
        Id                 = m.Id,
        BusinessName       = m.BusinessName,
        Cuit               = m.Cuit,
        Address            = m.Address,
        Latitude           = m.Latitude,
        Longitude          = m.Longitude,
        ContactPhone       = m.ContactPhone,
        MpConnectionStatus = m.MpConnectionStatus,
        Categories         = m.MerchantCategories
                               .Select(mc => new CategoryResponse { Id = mc.CategoryId, Name = mc.Category.Name })
                               .ToList()
    };
}
