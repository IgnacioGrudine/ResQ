using FluentResults;
using ResQ.API.DTOs.Products;

namespace ResQ.API.Services.Products;

public interface IProductService
{
    /// <summary>
    /// Returns all products (surprise packs) that belong to the authenticated merchant,
    /// regardless of their active/inactive status.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile whose products are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a collection of <see cref="ProductResponse"/>
    /// items; or a failed result if the profile does not exist.
    /// </returns>
    Task<Result<IEnumerable<ProductResponse>>> GetMyProductsAsync(int merchantProfileId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new surprise pack product for the authenticated merchant.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile that owns the new product.</param>
    /// <param name="request">DTO containing the product name, description, price, stock quantity, and pickup time window.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing the newly created <see cref="ProductResponse"/>;
    /// or a failed result if the request data is invalid or the profile does not exist.
    /// </returns>
    Task<Result<ProductResponse>> CreateProductAsync(int merchantProfileId, CreateProductRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates the mutable fields of an existing product belonging to the authenticated merchant.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile that owns the product.</param>
    /// <param name="productId">The identifier of the product to update.</param>
    /// <param name="request">DTO containing the fields to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing the updated <see cref="ProductResponse"/>;
    /// or a failed result if the product does not exist or does not belong to this merchant.
    /// </returns>
    Task<Result<ProductResponse>> UpdateProductAsync(int merchantProfileId, int productId, UpdateProductRequest request, CancellationToken ct = default);

    /// <summary>
    /// Permanently deletes a product belonging to the authenticated merchant.
    /// </summary>
    /// <remarks>
    /// Only products with no associated completed orders should be deleted. Active products
    /// with existing order history should be deactivated instead using <see cref="ToggleProductAsync"/>.
    /// </remarks>
    /// <param name="merchantProfileId">The identifier of the merchant profile that owns the product.</param>
    /// <param name="productId">The identifier of the product to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result"/> if the product was deleted; or a failed result if the
    /// product does not exist or does not belong to this merchant.
    /// </returns>
    Task<Result> DeleteProductAsync(int merchantProfileId, int productId, CancellationToken ct = default);

    /// <summary>
    /// Toggles the active/inactive status of a product belonging to the authenticated merchant.
    /// Inactive products are hidden from the public catalog and cannot be ordered.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile that owns the product.</param>
    /// <param name="productId">The identifier of the product whose status is to be toggled.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing the updated <see cref="ProductResponse"/>
    /// reflecting the new active status; or a failed result if the product does not exist or
    /// does not belong to this merchant.
    /// </returns>
    Task<Result<ProductResponse>> ToggleProductAsync(int merchantProfileId, int productId, CancellationToken ct = default);

    /// <summary>
    /// Uploads and stores an image for a specific product belonging to the authenticated
    /// merchant, replacing any previously stored image.
    /// </summary>
    /// <param name="merchantProfileId">The identifier of the merchant profile that owns the product.</param>
    /// <param name="productId">The identifier of the product whose image is being updated.</param>
    /// <param name="file">The image file to upload. Accepted formats and maximum size are enforced by the storage service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing the updated <see cref="ProductResponse"/>
    /// with the new image URL; or a failed result if the product does not exist, does not belong
    /// to this merchant, or the upload fails.
    /// </returns>
    Task<Result<ProductResponse>> UploadImageAsync(int merchantProfileId, int productId, IFormFile file, CancellationToken ct = default);
}
