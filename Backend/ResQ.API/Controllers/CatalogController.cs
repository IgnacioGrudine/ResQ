using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.Catalog;
using ResQ.API.DTOs.Merchants;
using ResQ.API.DTOs.Shared;
using ResQ.API.Extensions;
using ResQ.API.Services.Catalog;

namespace ResQ.API.Controllers;

/// <summary>
/// Provides public, unauthenticated read access to the ResQ catalog.
/// Consumers use these endpoints to browse merchants and available packs before placing an order.
/// No authorization is required for any endpoint in this controller.
/// </summary>
[ApiController]
[Route("api/catalog")]
public class CatalogController(ICatalogService catalogService) : ControllerBase
{
    /// <summary>
    /// Returns all available product categories.
    /// Used to populate filter dropdowns in the consumer-facing pack browser.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with a list of <see cref="CategoryResponse"/> items.</returns>
    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetCategories(CancellationToken ct)
        => (await catalogService.GetCategoriesAsync(ct)).ToActionResult();

    /// <summary>
    /// Returns the list of merchants that have at least one active pack available today.
    /// Used to populate the main catalog view for consumers.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with a list of <see cref="MerchantListItemResponse"/> items.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MerchantListItemResponse>>> GetCatalog(CancellationToken ct)
        => (await catalogService.GetCatalogAsync(ct)).ToActionResult();

    /// <summary>
    /// Returns the public profile of a single merchant, including their active products
    /// and a sample of recent reviews.
    /// </summary>
    /// <param name="id">The ResQ internal ID of the merchant profile.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with a <see cref="MerchantDetailResponse"/>; 404 Not Found if the merchant does not exist.
    /// </returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MerchantDetailResponse>> GetMerchantDetail(int id, CancellationToken ct)
        => (await catalogService.GetMerchantDetailAsync(id, ct)).ToActionResult();

    /// <summary>
    /// Returns active packs across all merchants with optional proximity sorting and filters.
    /// Pass <paramref name="lat"/> and <paramref name="lon"/> to receive distance-sorted results.
    /// All query parameters are optional and can be combined freely.
    /// </summary>
    /// <param name="lat">Consumer's latitude for proximity sorting. Requires <paramref name="lon"/>.</param>
    /// <param name="lon">Consumer's longitude for proximity sorting. Requires <paramref name="lat"/>.</param>
    /// <param name="search">Free-text search term matched against pack and merchant names.</param>
    /// <param name="categoryId">Filters results to packs belonging to the specified category.</param>
    /// <param name="maxPrice">Filters out packs priced above this value (in ARS).</param>
    /// <param name="maxDistance">
    /// Filters out merchants farther than this distance in kilometres.
    /// Only effective when <paramref name="lat"/> and <paramref name="lon"/> are also provided.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with a filtered and optionally sorted list of <see cref="PackListItemResponse"/> items.</returns>
    [HttpGet("packs")]
    public async Task<ActionResult<IEnumerable<PackListItemResponse>>> GetPacks(
        [FromQuery] double? lat,
        [FromQuery] double? lon,
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] decimal? maxPrice,
        [FromQuery] double? maxDistance,
        CancellationToken ct)
        => (await catalogService.GetPacksAsync(lat, lon, search, categoryId, maxPrice, maxDistance, ct)).ToActionResult();

    /// <summary>
    /// Returns a single pack by its ID.
    /// To retrieve full merchant context (address, reviews, other packs) call
    /// <c>GET /api/catalog/{merchantId}</c> instead.
    /// </summary>
    /// <param name="id">The ResQ internal ID of the pack (product).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with a <see cref="PackListItemResponse"/>; 404 Not Found if the pack does not exist or is inactive.
    /// </returns>
    [HttpGet("packs/{id:int}")]
    public async Task<ActionResult<PackListItemResponse>> GetPackById(int id, CancellationToken ct)
        => (await catalogService.GetPackByIdAsync(id, ct)).ToActionResult();
}
