using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.Products;
using ResQ.API.Extensions;
using ResQ.API.Services.Products;

namespace ResQ.API.Controllers;

[ApiController]
[Route("api/merchants/me/products")]
[Authorize(Roles = "Merchant")]
public class ProductsController(IProductService productService) : ControllerBase
{
    /// <summary>Returns all packs (active and inactive) belonging to the authenticated merchant.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetMyProducts(CancellationToken ct)
        => (await productService.GetMyProductsAsync(User.GetProfileId(), ct)).ToActionResult();

    /// <summary>Creates a new pack for the authenticated merchant.</summary>
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> CreateProduct(
        [FromBody] CreateProductRequest request, CancellationToken ct)
        => (await productService.CreateProductAsync(User.GetProfileId(), request, ct)).ToCreatedResult(x => x);

    /// <summary>Updates an existing pack. Returns 404 if the pack does not belong to this merchant.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(
        int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
        => (await productService.UpdateProductAsync(User.GetProfileId(), id, request, ct)).ToActionResult();

    /// <summary>Permanently deletes a pack. Returns 404 if the pack does not belong to this merchant.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken ct)
        => (await productService.DeleteProductAsync(User.GetProfileId(), id, ct)).ToActionResult();

    /// <summary>Toggles the IsActive flag of a pack (activate/deactivate).</summary>
    [HttpPatch("{id:int}/toggle")]
    public async Task<ActionResult<ProductResponse>> ToggleProduct(int id, CancellationToken ct)
        => (await productService.ToggleProductAsync(User.GetProfileId(), id, ct)).ToActionResult();
}
