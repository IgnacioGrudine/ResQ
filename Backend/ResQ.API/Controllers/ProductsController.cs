using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.Products;
using ResQ.API.Extensions;
using ResQ.API.Services.Products;

namespace ResQ.API.Controllers;

/// <summary>
/// Manages the authenticated merchant's pack (product) catalog.
/// All endpoints require a valid JWT with role <c>Merchant</c>.
/// Every operation is scoped to the calling merchant — a merchant cannot view or modify
/// another merchant's packs. The merchant's profile ID is extracted from JWT claims
/// via <c>User.GetProfileId()</c>.
/// </summary>
[ApiController]
[Route("api/merchants/me/products")]
[Authorize(Roles = "Merchant")]
public class ProductsController(IProductService productService) : ControllerBase
{
    /// <summary>
    /// Returns all packs belonging to the authenticated merchant, including both active and inactive ones.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with a list of <see cref="ProductResponse"/> items; empty list if none exist.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetMyProducts(CancellationToken ct)
        => (await productService.GetMyProductsAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>
    /// Creates a new surplus food pack for the authenticated merchant.
    /// The pack is inactive by default and must be explicitly activated before appearing in the catalog.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <remarks>
    /// The merchant must have an active Mercado Pago connection before activating a pack,
    /// since payments require a valid access token to create checkout preferences.
    /// </remarks>
    /// <param name="request">The pack details: name, description, price, quantity, and category.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 Created with the new <see cref="ProductResponse"/>.</returns>
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> CreateProduct(
        [FromBody] CreateProductRequest request, CancellationToken ct)
        => (await productService.CreateProductAsync(User.GetProfileId(), request, ct)).ToCreatedResult(x => x);

    /// <summary>
    /// Updates the details of an existing pack owned by the authenticated merchant.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="id">The ResQ internal ID of the pack to update.</param>
    /// <param name="request">The updated pack fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with the updated <see cref="ProductResponse"/>;
    /// 404 Not Found if the pack does not exist or does not belong to the authenticated merchant.
    /// </returns>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(
        int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
        => (await productService.UpdateProductAsync(User.GetProfileId(), id, request, ct)).ToActionResult();

    /// <summary>
    /// Permanently deletes a pack owned by the authenticated merchant.
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="id">The ResQ internal ID of the pack to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 204 No Content on success;
    /// 404 Not Found if the pack does not exist or does not belong to the authenticated merchant.
    /// </returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken ct)
        => (await productService.DeleteProductAsync(User.GetProfileId(), id, ct)).ToActionResult();

    /// <summary>
    /// Toggles the <c>IsActive</c> flag of a pack, switching it between active (visible in catalog)
    /// and inactive (hidden from consumers).
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <remarks>
    /// Attempting to activate a pack when the merchant's Mercado Pago account is disconnected
    /// will return an error, since payment collection requires a valid access token.
    /// </remarks>
    /// <param name="id">The ResQ internal ID of the pack to toggle.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with the updated <see cref="ProductResponse"/> reflecting the new <c>IsActive</c> state;
    /// 404 Not Found if the pack does not exist or does not belong to the authenticated merchant.
    /// </returns>
    [HttpPatch("{id:int}/toggle")]
    public async Task<ActionResult<ProductResponse>> ToggleProduct(int id, CancellationToken ct)
        => (await productService.ToggleProductAsync(User.GetProfileId(), id, ct)).ToActionResult();

    /// <summary>
    /// Uploads or replaces the image for a pack owned by the authenticated merchant.
    /// Accepted formats: JPG, PNG, WebP (max 5 MB).
    /// Requires role: <c>Merchant</c>.
    /// </summary>
    /// <param name="id">The ResQ internal ID of the pack whose image should be updated.</param>
    /// <param name="file">The image file sent as <c>multipart/form-data</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with the updated <see cref="ProductResponse"/> including the new image URL;
    /// 404 Not Found if the pack does not exist or does not belong to the authenticated merchant;
    /// 400 Bad Request if the file format or size is invalid.
    /// </returns>
    [HttpPut("{id:int}/image")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ProductResponse>> UploadImage(
        int id, IFormFile file, CancellationToken ct)
        => (await productService.UploadImageAsync(User.GetProfileId(), id, file, ct)).ToActionResult();
}
