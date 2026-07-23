using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ResQ.API.Common.Errors;
using ResQ.API.Controllers;
using ResQ.API.DTOs.Consumers;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Reviews;
using ResQ.API.Services.Consumers;
using ResQ.API.Services.Reviews;

namespace ResQ.Tests.Controllers;

public class ConsumersControllerTests
{
    private const int ConsumerProfileId = 20;

    private readonly Mock<IConsumerService> _consumerService = new();
    private readonly Mock<IReviewService> _reviewService = new();

    private ConsumersController CreateSut()
    {
        var claims = new[]
        {
            new Claim("profileId", ConsumerProfileId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, "200")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        return new ConsumersController(_consumerService.Object, _reviewService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private static ConsumerProfileResponse BuildProfileResponse() => new()
    {
        Id          = ConsumerProfileId,
        FirstName   = "Ana",
        LastName    = "Pérez",
        Email       = "ana@example.com",
        PhoneNumber = "351-1112222",
        TotalOrders = 3,
        TotalSaved  = 1500m,
        Co2SavedKg  = 1.5m
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyProfile
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyProfile_WhenServiceReturnsOk_Returns200WithProfile()
    {
        // Arrange
        var response = BuildProfileResponse();
        _consumerService.Setup(s => s.GetMyProfileAsync(ConsumerProfileId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMyProfile(CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<ConsumerProfileResponse>(result.Value);
        Assert.Equal("Ana", returned.FirstName);
    }

    [Fact]
    public async Task GetMyProfile_WhenNotFound_Returns404()
    {
        // Arrange
        _consumerService.Setup(s => s.GetMyProfileAsync(ConsumerProfileId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Fail(new NotFoundError("Perfil de consumidor no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMyProfile(CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdateMyProfile
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateMyProfile_WhenServiceReturnsOk_Returns200WithUpdatedProfile()
    {
        // Arrange
        var response = BuildProfileResponse();
        response.FirstName = "Ana María";
        _consumerService.Setup(s => s.UpdateMyProfileAsync(ConsumerProfileId, It.IsAny<UpdateConsumerProfileRequest>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.UpdateMyProfile(new UpdateConsumerProfileRequest(), CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<ConsumerProfileResponse>(result.Value);
        Assert.Equal("Ana María", returned.FirstName);
    }

    [Fact]
    public async Task UpdateMyProfile_WhenNotFound_Returns404()
    {
        // Arrange
        _consumerService.Setup(s => s.UpdateMyProfileAsync(ConsumerProfileId, It.IsAny<UpdateConsumerProfileRequest>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Fail(new NotFoundError("Perfil de consumidor no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.UpdateMyProfile(new UpdateConsumerProfileRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyOrders
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyOrders_WhenServiceReturnsOk_Returns200WithOrders()
    {
        // Arrange
        var orders = new List<OrderSummaryResponse> { new() { Id = 1 }, new() { Id = 2 } };
        _consumerService.Setup(s => s.GetMyOrdersAsync(ConsumerProfileId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result.Ok<IEnumerable<OrderSummaryResponse>>(orders));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMyOrders(CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<OrderSummaryResponse>>(result.Value).ToList();
        Assert.Equal(2, items.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateReview
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateReview_WhenServiceReturnsOk_Returns201WithReview()
    {
        // Arrange
        var response = new ReviewResponse { Id = 1, Rating = 5, Comment = "Excelente" };
        _reviewService.Setup(s => s.CreateReviewAsync(ConsumerProfileId, 7, It.IsAny<CreateReviewRequest>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.CreateReview(7, new CreateReviewRequest { Rating = 5, Comment = "Excelente" }, CancellationToken.None);

        // Assert
        var result = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var returned = Assert.IsType<ReviewResponse>(result.Value);
        Assert.Equal(5, returned.Rating);
    }

    [Fact]
    public async Task CreateReview_WhenOrderNotFound_Returns404()
    {
        // Arrange
        _reviewService.Setup(s => s.CreateReviewAsync(ConsumerProfileId, 99, It.IsAny<CreateReviewRequest>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(Result.Fail(new NotFoundError("Orden no encontrada.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.CreateReview(99, new CreateReviewRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task CreateReview_WhenReviewAlreadyExists_Returns409Conflict()
    {
        // Arrange
        _reviewService.Setup(s => s.CreateReviewAsync(ConsumerProfileId, 7, It.IsAny<CreateReviewRequest>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(Result.Fail(new ConflictError("Ya existe una reseña para esta orden.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.CreateReview(7, new CreateReviewRequest(), CancellationToken.None);

        // Assert
        Assert.IsType<ConflictObjectResult>(actionResult.Result);
    }
}
