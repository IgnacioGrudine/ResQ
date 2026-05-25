using FluentResults;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Products;
using ResQ.API.Models.Catalog;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Services.Storage;

namespace ResQ.API.Services.Products;

public class ProductService(IProductRepository products, IImageStorageService imageStorage) : IProductService
{
    public async Task<Result<IEnumerable<ProductResponse>>> GetMyProductsAsync(int merchantProfileId, CancellationToken ct = default)
    {
        var result = await products.GetByMerchantIdAsync(merchantProfileId, ct);
        return Result.Ok(result.Select(MapProduct));
    }

    public async Task<Result<ProductResponse>> CreateProductAsync(
        int merchantProfileId, CreateProductRequest request, CancellationToken ct = default)
    {
        var product = new Product
        {
            MerchantId      = merchantProfileId,
            Name            = request.Name,
            Description     = request.Description,
            ImageUrl        = request.ImageUrl,
            ProductType     = request.ProductType,
            OriginalPrice   = request.OriginalPrice,
            SalePrice       = request.SalePrice,
            StockQuantity   = request.StockQuantity,
            PickupTimeStart = request.PickupTimeStart,
            PickupTimeEnd   = request.PickupTimeEnd,
            IsActive        = true,
            CreatedAt       = DateTime.UtcNow
        };

        await products.AddAsync(product, ct);
        await products.SaveChangesAsync(ct);

        return Result.Ok(MapProduct(product));
    }

    public async Task<Result<ProductResponse>> UpdateProductAsync(
        int merchantProfileId, int productId, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await products.GetByIdForMerchantAsync(productId, merchantProfileId, ct);
        if (product is null)
            return Result.Fail(new NotFoundError("Pack no encontrado."));

        product.Name            = request.Name;
        product.Description     = request.Description;
        product.ImageUrl        = request.ImageUrl;
        product.ProductType     = request.ProductType;
        product.OriginalPrice   = request.OriginalPrice;
        product.SalePrice       = request.SalePrice;
        product.StockQuantity   = request.StockQuantity;
        product.PickupTimeStart = request.PickupTimeStart;
        product.PickupTimeEnd   = request.PickupTimeEnd;
        product.UpdatedAt       = DateTime.UtcNow;
        products.Update(product);

        await products.SaveChangesAsync(ct);
        return Result.Ok(MapProduct(product));
    }

    public async Task<Result> DeleteProductAsync(int merchantProfileId, int productId, CancellationToken ct = default)
    {
        var product = await products.GetByIdForMerchantAsync(productId, merchantProfileId, ct);
        if (product is null)
            return Result.Fail(new NotFoundError("Pack no encontrado."));

        products.Delete(product);
        await products.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<ProductResponse>> ToggleProductAsync(int merchantProfileId, int productId, CancellationToken ct = default)
    {
        var product = await products.GetByIdForMerchantAsync(productId, merchantProfileId, ct);
        if (product is null)
            return Result.Fail(new NotFoundError("Pack no encontrado."));

        product.IsActive  = !product.IsActive;
        product.UpdatedAt = DateTime.UtcNow;
        products.Update(product);

        await products.SaveChangesAsync(ct);
        return Result.Ok(MapProduct(product));
    }

    public async Task<Result<ProductResponse>> UploadImageAsync(
        int merchantProfileId, int productId, IFormFile file, CancellationToken ct = default)
    {
        var product = await products.GetByIdForMerchantAsync(productId, merchantProfileId, ct);
        if (product is null)
            return Result.Fail(new NotFoundError("Pack no encontrado."));

        var imageUrl = await imageStorage.UploadAsync(file, $"products/{merchantProfileId}", ct);

        product.ImageUrl  = imageUrl;
        product.UpdatedAt = DateTime.UtcNow;
        products.Update(product);
        await products.SaveChangesAsync(ct);

        return Result.Ok(MapProduct(product));
    }

    private static ProductResponse MapProduct(Product p) => new()
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
}
