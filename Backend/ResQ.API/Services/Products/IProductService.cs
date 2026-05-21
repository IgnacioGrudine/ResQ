using FluentResults;
using ResQ.API.DTOs.Products;

namespace ResQ.API.Services.Products;

public interface IProductService
{
    Task<Result<IEnumerable<ProductResponse>>> GetMyProductsAsync(int merchantProfileId, CancellationToken ct = default);
    Task<Result<ProductResponse>> CreateProductAsync(int merchantProfileId, CreateProductRequest request, CancellationToken ct = default);
    Task<Result<ProductResponse>> UpdateProductAsync(int merchantProfileId, int productId, UpdateProductRequest request, CancellationToken ct = default);
    Task<Result> DeleteProductAsync(int merchantProfileId, int productId, CancellationToken ct = default);
    Task<Result<ProductResponse>> ToggleProductAsync(int merchantProfileId, int productId, CancellationToken ct = default);
}
