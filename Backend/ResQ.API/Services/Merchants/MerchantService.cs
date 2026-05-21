using FluentResults;
using ResQ.API.Common.Errors;
using ResQ.API.Data.UnitOfWork;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Products;
using ResQ.API.DTOs.Reviews;
using ResQ.API.DTOs.Shared;
using ResQ.API.Models.Catalog;

namespace ResQ.API.Services.Merchants;

public class MerchantService(IUnitOfWork uow) : IMerchantService
{
    // ─── Public catalog ───────────────────────────────────────────────────────

    public async Task<Result<IEnumerable<MerchantListItemResponse>>> GetCatalogAsync(CancellationToken ct = default)
    {
        var merchants = await uow.MerchantProfiles.GetAllWithCatalogDataAsync(ct);

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
        var merchant = await uow.MerchantProfiles.GetByIdWithPublicDetailAsync(merchantId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Comercio no encontrado."));

        var response = new MerchantDetailResponse
        {
            Id            = merchant.Id,
            BusinessName  = merchant.BusinessName,
            Address       = merchant.Address,
            Latitude      = merchant.Latitude,
            Longitude     = merchant.Longitude,
            ContactPhone  = merchant.ContactPhone,
            Categories    = merchant.MerchantCategories
                                .Select(mc => new CategoryResponse { Id = mc.CategoryId, Name = mc.Category.Name })
                                .ToList(),
            AverageRating = merchant.Reviews.Any() ? Math.Round((decimal)merchant.Reviews.Average(r => r.Rating), 1) : 0,
            ReviewCount   = merchant.Reviews.Count,
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
        };

        return Result.Ok(response);
    }

    // ─── Authenticated merchant — own profile ─────────────────────────────────

    public async Task<Result<MerchantProfileResponse>> GetMyProfileAsync(int merchantProfileId, CancellationToken ct = default)
    {
        var merchant = await uow.MerchantProfiles.GetByIdWithCategoriesAsync(merchantProfileId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Perfil de comercio no encontrado."));

        return Result.Ok(MapMerchantProfile(merchant));
    }

    public async Task<Result<MerchantProfileResponse>> UpdateMyProfileAsync(
        int merchantProfileId, UpdateMerchantProfileRequest request, CancellationToken ct = default)
    {
        var merchant = await uow.MerchantProfiles.GetByIdAsync(merchantProfileId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Perfil de comercio no encontrado."));

        merchant.BusinessName = request.BusinessName;
        merchant.Address      = request.Address;
        merchant.Latitude     = request.Latitude;
        merchant.Longitude    = request.Longitude;
        merchant.ContactPhone = request.ContactPhone;
        merchant.UpdatedAt    = DateTime.UtcNow;
        uow.MerchantProfiles.Update(merchant);

        // Replace categories if provided
        if (request.CategoryIds.Count > 0)
        {
            var existing = await uow.MerchantCategories.GetByMerchantIdAsync(merchantProfileId, ct);
            uow.MerchantCategories.DeleteRange(existing);

            foreach (var catId in request.CategoryIds.Distinct())
                await uow.MerchantCategories.AddAsync(
                    new MerchantCategory { MerchantId = merchantProfileId, CategoryId = catId }, ct);
        }

        await uow.SaveChangesAsync(ct);

        var updated = await uow.MerchantProfiles.GetByIdWithCategoriesAsync(merchantProfileId, ct);
        return Result.Ok(MapMerchantProfile(updated!));
    }

    // ─── Authenticated merchant — orders ─────────────────────────────────────

    public async Task<Result<IEnumerable<MerchantOrderSummaryResponse>>> GetMyOrdersAsync(
        int merchantProfileId, CancellationToken ct = default)
    {
        var orders = await uow.Orders.GetByMerchantIdAsync(merchantProfileId, ct);

        var result = orders.Select(o => new MerchantOrderSummaryResponse
        {
            Id           = o.Id,
            ConsumerName = $"{o.Consumer.FirstName} {o.Consumer.LastName}",
            TotalAmount  = o.TotalAmount,
            OrderStatus  = o.OrderStatus.ToString(),
            PickupCode   = o.PickupCode,
            CreatedAt    = o.CreatedAt,
            Items        = o.OrderDetails.Select(od => new OrderDetailItemResponse
            {
                ProductName = od.Product.Name,
                Quantity    = od.Quantity,
                UnitPrice   = od.UnitPrice
            }).ToList()
        });

        return Result.Ok(result);
    }

    // ─── Authenticated merchant — reviews ────────────────────────────────────

    public async Task<Result<IEnumerable<ReviewResponse>>> GetMyReviewsAsync(
        int merchantProfileId, CancellationToken ct = default)
    {
        var reviews = await uow.Reviews.GetByMerchantIdAsync(merchantProfileId, ct);

        var result = reviews.Select(r => new ReviewResponse
        {
            Id        = r.Id,
            Rating    = r.Rating,
            Comment   = r.Comment,
            CreatedAt = r.CreatedAt
        });

        return Result.Ok(result);
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
