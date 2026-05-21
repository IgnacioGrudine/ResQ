using Microsoft.AspNetCore.Mvc;
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
}
