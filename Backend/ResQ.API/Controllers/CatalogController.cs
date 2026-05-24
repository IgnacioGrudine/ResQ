using Microsoft.AspNetCore.Mvc;
using ResQ.API.DTOs.Catalog;
using ResQ.API.DTOs.Merchants;
using ResQ.API.Extensions;
using ResQ.API.Services.Catalog;

namespace ResQ.API.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController(ICatalogService catalogService) : ControllerBase
{
    /// <summary>Returns the list of merchants with active packs available today.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MerchantListItemResponse>>> GetCatalog(CancellationToken ct)
        => (await catalogService.GetCatalogAsync(ct)).ToActionResult();

    /// <summary>Returns the public detail of a merchant including active products and recent reviews.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MerchantDetailResponse>> GetMerchantDetail(int id, CancellationToken ct)
        => (await catalogService.GetMerchantDetailAsync(id, ct)).ToActionResult();

    /// <summary>
    /// Returns active packs across all merchants with optional proximity sorting and filters.
    /// Pass lat/lon to get distance-sorted results. All query params are optional.
    /// </summary>
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

    /// <summary>Returns the pack data by ID. To get full merchant context call GET /api/catalog/{merchantId}.</summary>
    [HttpGet("packs/{id:int}")]
    public async Task<ActionResult<PackListItemResponse>> GetPackById(int id, CancellationToken ct)
        => (await catalogService.GetPackByIdAsync(id, ct)).ToActionResult();
}
