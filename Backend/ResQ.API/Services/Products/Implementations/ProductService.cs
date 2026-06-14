using FluentResults;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Products;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Services.Storage;

namespace ResQ.API.Services.Products;

/// <summary>
/// Manages the full lifecycle of a merchant's surprise food packs (products):
/// listing, creation, updating, deletion, visibility toggling, and image upload.
/// </summary>
/// <remarks>
/// All write operations are scoped to the authenticated merchant's profile ID to prevent
/// cross-merchant data access. Ownership is enforced at the repository level via
/// <c>GetByIdForMerchantAsync</c>, which returns <c>null</c> when the product does not
/// belong to the requesting merchant.
/// <para>
/// Publishing a pack (creating one, or activating an existing one) requires the merchant
/// to have a live Mercado Pago connection, since a valid access token is needed to create
/// checkout preferences. This rule is enforced via <see cref="EnsureMpConnectedAsync"/>.
/// </para>
/// </remarks>
public class ProductService(
    IProductRepository products,
    IMerchantProfileRepository merchants,
    IImageStorageService imageStorage) : IProductService
{
    /// <summary>
    /// Returns all products (active and inactive) belonging to the authenticated merchant.
    /// </summary>
    /// <param name="merchantProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing an enumerable of <see cref="ProductResponse"/>.
    /// </returns>
    public async Task<Result<IEnumerable<ProductResponse>>> GetMyProductsAsync(int merchantProfileId, CancellationToken ct = default)
    {
        var result = await products.GetByMerchantIdAsync(merchantProfileId, ct);
        return Result.Ok(result.Select(MapProduct));
    }

    /// <summary>
    /// Creates a new surprise food pack for the authenticated merchant and persists it
    /// with an initial active state.
    /// </summary>
    /// <param name="merchantProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="request">The creation payload containing all pack details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{ProductResponse}"/> with the newly created pack.
    /// </returns>
    public async Task<Result<ProductResponse>> CreateProductAsync(
        int merchantProfileId, CreateProductRequest request, CancellationToken ct = default)
    {
        var gate = await EnsureMpConnectedAsync(merchantProfileId, ct);
        if (gate.IsFailed)
            return Result.Fail<ProductResponse>(gate.Errors);

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

    /// <summary>
    /// Updates all editable fields of an existing pack owned by the authenticated merchant.
    /// </summary>
    /// <param name="merchantProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="productId">The ID of the pack to update.</param>
    /// <param name="request">The update payload containing the new field values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{ProductResponse}"/> with the updated pack,
    /// or a <c>NotFoundError</c> if the pack does not exist or does not belong to this merchant.
    /// </returns>
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

    /// <summary>
    /// Permanently deletes a pack owned by the authenticated merchant.
    /// </summary>
    /// <param name="merchantProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="productId">The ID of the pack to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result"/>,
    /// or a <c>NotFoundError</c> if the pack does not exist or does not belong to this merchant.
    /// </returns>
    public async Task<Result> DeleteProductAsync(int merchantProfileId, int productId, CancellationToken ct = default)
    {
        var product = await products.GetByIdForMerchantAsync(productId, merchantProfileId, ct);
        if (product is null)
            return Result.Fail(new NotFoundError("Pack no encontrado."));

        products.Delete(product);
        await products.SaveChangesAsync(ct);
        return Result.Ok();
    }

    /// <summary>
    /// Toggles the <c>IsActive</c> flag of a pack owned by the authenticated merchant,
    /// making it visible or hidden in the public catalog.
    /// </summary>
    /// <param name="merchantProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="productId">The ID of the pack to toggle.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{ProductResponse}"/> with the updated pack (reflecting the new <c>IsActive</c> value),
    /// or a <c>NotFoundError</c> if the pack does not exist or does not belong to this merchant.
    /// </returns>
    public async Task<Result<ProductResponse>> ToggleProductAsync(int merchantProfileId, int productId, CancellationToken ct = default)
    {
        var product = await products.GetByIdForMerchantAsync(productId, merchantProfileId, ct);
        if (product is null)
            return Result.Fail(new NotFoundError("Pack no encontrado."));

        // Activating a pack makes it visible in the catalog, so it must be payable.
        // Deactivating is always allowed (e.g. when the merchant disconnects Mercado Pago).
        if (!product.IsActive)
        {
            var gate = await EnsureMpConnectedAsync(merchantProfileId, ct);
            if (gate.IsFailed)
                return Result.Fail<ProductResponse>(gate.Errors);
        }

        product.IsActive  = !product.IsActive;
        product.UpdatedAt = DateTime.UtcNow;
        products.Update(product);

        await products.SaveChangesAsync(ct);
        return Result.Ok(MapProduct(product));
    }

    /// <summary>
    /// Uploads an image for a specific pack to MinIO object storage and persists the
    /// resulting public URL on the product entity.
    /// </summary>
    /// <param name="merchantProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="productId">The ID of the pack whose image should be updated.</param>
    /// <param name="file">The image file to upload. Validated for allowed content type and size by <see cref="IImageStorageService"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{ProductResponse}"/> with the updated pack (including the new image URL),
    /// or a <c>NotFoundError</c> if the pack does not exist or does not belong to this merchant.
    /// </returns>
    /// <remarks>
    /// Images are stored under the <c>products/{merchantProfileId}</c> folder in the configured MinIO bucket.
    /// Uploading a new image overwrites the <c>ImageUrl</c> field but does not delete the previously stored object.
    /// </remarks>
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

    /// <summary>
    /// Verifies that the merchant has an active Mercado Pago connection before allowing
    /// a pack to be published. Payments require a valid access token to create checkout
    /// preferences, so a pack cannot go live while the merchant is disconnected.
    /// </summary>
    /// <param name="merchantProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result"/> when the merchant's Mercado Pago account is connected;
    /// a <c>NotFoundError</c> if the merchant profile does not exist;
    /// a <c>BadRequestError</c> if the Mercado Pago account is not connected.
    /// </returns>
    private async Task<Result> EnsureMpConnectedAsync(int merchantProfileId, CancellationToken ct)
    {
        var merchant = await merchants.GetByIdAsync(merchantProfileId, ct);
        if (merchant is null)
            return Result.Fail(new NotFoundError("Comercio no encontrado."));

        if (merchant.MpConnectionStatus != MpConnectionStatus.Connected)
            return Result.Fail(new BadRequestError(
                "Necesitás vincular tu cuenta de Mercado Pago antes de publicar packs."));

        return Result.Ok();
    }

    /// <summary>
    /// Maps a <see cref="Product"/> entity to a <see cref="ProductResponse"/> DTO.
    /// </summary>
    /// <param name="p">The product entity to map.</param>
    /// <returns>A populated <see cref="ProductResponse"/> DTO.</returns>
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
