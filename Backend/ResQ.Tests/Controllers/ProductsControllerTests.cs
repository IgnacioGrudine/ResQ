using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ResQ.API.Common.Errors;
using ResQ.API.Controllers;
using ResQ.API.DTOs.Products;
using ResQ.API.Models.Enums;
using ResQ.API.Services.Products;

namespace ResQ.Tests.Controllers;

public class ProductsControllerTests
{
    private const int MerchantProfileId = 42;

    private readonly Mock<IProductService> _productService = new();

    private ProductsController CreateSut()
    {
        var claims = new[]
        {
            new Claim("profileId", MerchantProfileId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, "100")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        return new ProductsController(_productService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private static ProductResponse BuildProductResponse(int id = 1) => new()
    {
        Id              = id,
        Name            = "Pack Sorpresa",
        ProductType     = ProductType.SurprisePack.ToString(),
        OriginalPrice   = 1000m,
        SalePrice       = 400m,
        StockQuantity   = 5,
        PickupTimeStart = new TimeOnly(18, 0),
        PickupTimeEnd   = new TimeOnly(21, 0),
        IsActive        = true
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyProducts
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyProducts_WhenServiceReturnsOk_Returns200WithProducts()
    {
        // Arrange
        var products = new List<ProductResponse> { BuildProductResponse(1), BuildProductResponse(2) };
        _productService.Setup(s => s.GetMyProductsAsync(MerchantProfileId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok<IEnumerable<ProductResponse>>(products));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMyProducts(CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<ProductResponse>>(result.Value).ToList();
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetMyProducts_PassesMerchantProfileIdFromClaims()
    {
        // Arrange
        _productService.Setup(s => s.GetMyProductsAsync(MerchantProfileId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok<IEnumerable<ProductResponse>>([]));

        var sut = CreateSut();

        // Act
        await sut.GetMyProducts(CancellationToken.None);

        // Assert
        _productService.Verify(s => s.GetMyProductsAsync(MerchantProfileId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateProduct
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateProduct_WhenServiceReturnsOk_Returns201WithProduct()
    {
        // Arrange
        var response = BuildProductResponse();
        _productService.Setup(s => s.CreateProductAsync(MerchantProfileId, It.IsAny<CreateProductRequest>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.CreateProduct(new CreateProductRequest(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(201, result.StatusCode);
        Assert.IsType<ProductResponse>(result.Value);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdateProduct
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateProduct_WhenProductNotFound_Returns404()
    {
        // Arrange
        _productService.Setup(s => s.UpdateProductAsync(MerchantProfileId, 99, It.IsAny<UpdateProductRequest>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Fail(new NotFoundError("Pack no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.UpdateProduct(99, new UpdateProductRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task UpdateProduct_WhenServiceReturnsOk_Returns200WithUpdatedProduct()
    {
        // Arrange
        var response = BuildProductResponse();
        response.Name = "Nombre actualizado";
        _productService.Setup(s => s.UpdateProductAsync(MerchantProfileId, 1, It.IsAny<UpdateProductRequest>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.UpdateProduct(1, new UpdateProductRequest(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<ProductResponse>(result.Value);
        Assert.Equal("Nombre actualizado", returned.Name);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DeleteProduct
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteProduct_WhenProductNotFound_Returns404()
    {
        // Arrange
        _productService.Setup(s => s.DeleteProductAsync(MerchantProfileId, 99, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Fail(new NotFoundError("Pack no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.DeleteProduct(99, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult);
    }

    [Fact]
    public async Task DeleteProduct_WhenServiceReturnsOk_Returns204NoContent()
    {
        // Arrange
        _productService.Setup(s => s.DeleteProductAsync(MerchantProfileId, 1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok());

        var sut = CreateSut();

        // Act
        var actionResult = await sut.DeleteProduct(1, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(actionResult);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ToggleProduct
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ToggleProduct_WhenProductNotFound_Returns404()
    {
        // Arrange
        _productService.Setup(s => s.ToggleProductAsync(MerchantProfileId, 99, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Fail(new NotFoundError("Pack no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.ToggleProduct(99, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task ToggleProduct_WhenServiceReturnsOk_Returns200WithProduct()
    {
        // Arrange
        var response = BuildProductResponse();
        response.IsActive = false;
        _productService.Setup(s => s.ToggleProductAsync(MerchantProfileId, 1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.ToggleProduct(1, CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<ProductResponse>(result.Value);
        Assert.False(returned.IsActive);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UploadImage
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UploadImage_WhenProductNotFound_Returns404()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        _productService.Setup(s => s.UploadImageAsync(MerchantProfileId, 99, It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Fail(new NotFoundError("Pack no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.UploadImage(99, mockFile.Object, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task UploadImage_WhenServiceReturnsOk_Returns200WithUpdatedImageUrl()
    {
        // Arrange
        var response = BuildProductResponse();
        response.ImageUrl = "https://cdn.resq.com/products/1/img.jpg";
        var mockFile = new Mock<IFormFile>();
        _productService.Setup(s => s.UploadImageAsync(MerchantProfileId, 1, It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.UploadImage(1, mockFile.Object, CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<ProductResponse>(result.Value);
        Assert.Equal("https://cdn.resq.com/products/1/img.jpg", returned.ImageUrl);
    }
}
