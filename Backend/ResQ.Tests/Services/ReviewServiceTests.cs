using FluentResults;
using Moq;
using ResQ.API.DTOs.Reviews;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Orders;
using ResQ.API.Models.Reviews;
using ResQ.API.Repositories.Orders;
using ResQ.API.Repositories.Reviews;
using ResQ.API.Services.Reviews;

namespace ResQ.Tests.Services;

public class ReviewServiceTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IReviewRepository> _reviews = new();

    private ReviewService CreateSut() => new(_orders.Object, _reviews.Object);

    private static Order BuildOrder(
        int id = 1,
        int consumerId = 10,
        int merchantId = 20,
        OrderStatus status = OrderStatus.PickedUp) => new()
    {
        Id                = id,
        ConsumerId        = consumerId,
        MerchantId        = merchantId,
        TotalAmount       = 500m,
        PlatformFee       = 50m,
        MerchantEarnings  = 450m,
        ExternalReference = Guid.NewGuid().ToString(),
        OrderStatus       = status,
        PickupCode        = "ABC123",
        CreatedAt         = DateTime.UtcNow
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateReviewAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateReviewAsync_WhenOrderNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _orders.Setup(o => o.GetByIdAsync(99, It.IsAny<CancellationToken>()))
               .ReturnsAsync((Order?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateReviewAsync(10, 99, new CreateReviewRequest { Rating = 5 });

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrada", result.Errors[0].Message);
        _reviews.Verify(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateReviewAsync_WhenOrderBelongsToDifferentConsumer_ReturnsForbiddenError()
    {
        // Arrange
        var order = BuildOrder(consumerId: 10);
        _orders.Setup(o => o.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateReviewAsync(999, order.Id, new CreateReviewRequest { Rating = 5 });

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("permiso", result.Errors[0].Message);
        _reviews.Verify(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateReviewAsync_WhenOrderNotPickedUp_ReturnsBadRequestError()
    {
        // Arrange
        var order = BuildOrder(consumerId: 10, status: OrderStatus.Paid);
        _orders.Setup(o => o.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateReviewAsync(10, order.Id, new CreateReviewRequest { Rating = 5 });

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("retiraste", result.Errors[0].Message);
        _reviews.Verify(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateReviewAsync_WhenReviewAlreadyExistsForOrder_ReturnsConflictError()
    {
        // Arrange
        var order = BuildOrder(consumerId: 10, status: OrderStatus.PickedUp);
        _orders.Setup(o => o.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);
        _reviews.Setup(r => r.ExistsForOrderAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateReviewAsync(10, order.Id, new CreateReviewRequest { Rating = 5 });

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Ya dejaste una reseña", result.Errors[0].Message);
        _reviews.Verify(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateReviewAsync_WhenValid_CreatesReviewWithOrderAndMerchantIdAndReturnsOk()
    {
        // Arrange
        var order = BuildOrder(consumerId: 10, merchantId: 20, status: OrderStatus.PickedUp);
        _orders.Setup(o => o.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);
        _reviews.Setup(r => r.ExistsForOrderAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        _reviews.Setup(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        _reviews.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var request = new CreateReviewRequest { Rating = 4, Comment = "Muy bueno" };
        var sut = CreateSut();

        // Act
        var result = await sut.CreateReviewAsync(10, order.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Rating);
        Assert.Equal("Muy bueno", result.Value.Comment);
        _reviews.Verify(r => r.AddAsync(
            It.Is<Review>(rv => rv.OrderId == order.Id && rv.MerchantId == order.MerchantId && rv.Rating == 4),
            It.IsAny<CancellationToken>()), Times.Once);
        _reviews.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateReviewAsync_TrimsWhitespaceFromComment()
    {
        // Arrange
        var order = BuildOrder(consumerId: 10, status: OrderStatus.PickedUp);
        _orders.Setup(o => o.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);
        _reviews.Setup(r => r.ExistsForOrderAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        _reviews.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var request = new CreateReviewRequest { Rating = 5, Comment = "  Excelente atención  " };
        var sut = CreateSut();

        // Act
        var result = await sut.CreateReviewAsync(10, order.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Excelente atención", result.Value.Comment);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateReviewAsync_WhenCommentIsNullOrWhitespace_StoresNullComment(string? comment)
    {
        // Arrange
        var order = BuildOrder(consumerId: 10, status: OrderStatus.PickedUp);
        _orders.Setup(o => o.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);
        _reviews.Setup(r => r.ExistsForOrderAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        _reviews.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var request = new CreateReviewRequest { Rating = 3, Comment = comment };
        var sut = CreateSut();

        // Act
        var result = await sut.CreateReviewAsync(10, order.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Comment);
    }
}
