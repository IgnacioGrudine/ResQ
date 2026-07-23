using FluentResults;
using Moq;
using ResQ.API.DTOs.Consumers;
using ResQ.API.DTOs.Orders;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Orders;
using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Orders;
using ResQ.API.Services.Consumers;
using ResQ.API.Services.Orders;

namespace ResQ.Tests.Services;

public class ConsumerServiceTests
{
    private readonly Mock<IConsumerProfileRepository> _consumerProfiles = new();
    private readonly Mock<IOrderService> _orderService = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();

    private ConsumerService CreateSut() => new(_consumerProfiles.Object, _orderService.Object, _orderRepository.Object);

    // ─── Builders ─────────────────────────────────────────────────────────────

    private static ConsumerProfile BuildConsumer(int id = 20, string email = "ana@example.com") => new()
    {
        Id          = id,
        FirstName   = "Ana",
        LastName    = "Pérez",
        PhoneNumber = "351-1112222",
        User        = new User { Id = 1, Email = email }
    };

    private static Order BuildOrder(int id, OrderStatus status, IEnumerable<(decimal OriginalPrice, decimal UnitPrice, int Quantity)> items) => new()
    {
        Id           = id,
        OrderStatus  = status,
        OrderDetails = items.Select((it, i) => new OrderDetail
        {
            Id        = id * 100 + i,
            Quantity  = it.Quantity,
            UnitPrice = it.UnitPrice,
            Product   = new Product { Id = i + 1, OriginalPrice = it.OriginalPrice, SalePrice = it.UnitPrice }
        }).ToList()
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyProfileAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyProfileAsync_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _consumerProfiles.Setup(c => c.GetByIdWithUserAsync(99, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((ConsumerProfile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetMyProfileAsync(99);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task GetMyProfileAsync_WhenFound_ReturnsProfileWithComputedStats()
    {
        // Arrange
        var profile = BuildConsumer(20, "ana@example.com");
        var completedOrder = BuildOrder(1, OrderStatus.PickedUp, [(1000m, 400m, 2)]); // saved: 1200
        var cancelledOrder = BuildOrder(2, OrderStatus.Cancelled, [(1000m, 400m, 5)]); // excluded

        _consumerProfiles.Setup(c => c.GetByIdWithUserAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        _orderRepository.Setup(o => o.GetByConsumerIdAsync(20, It.IsAny<CancellationToken>()))
                         .ReturnsAsync([completedOrder, cancelledOrder]);

        var sut = CreateSut();

        // Act
        var result = await sut.GetMyProfileAsync(20);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.Equal(20, dto.Id);
        Assert.Equal("Ana", dto.FirstName);
        Assert.Equal("Pérez", dto.LastName);
        Assert.Equal("ana@example.com", dto.Email);
        Assert.Equal(1, dto.TotalOrders);
        Assert.Equal(1200m, dto.TotalSaved);
        Assert.Equal(0.5m, dto.Co2SavedKg);
    }

    [Fact]
    public async Task GetMyProfileAsync_WhenSavingsWouldBeNegative_ClampsTotalSavedToZero()
    {
        // Arrange
        var profile = BuildConsumer(20);
        var promoOrder = BuildOrder(1, OrderStatus.Paid, [(100m, 150m, 1)]); // saved: -50

        _consumerProfiles.Setup(c => c.GetByIdWithUserAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        _orderRepository.Setup(o => o.GetByConsumerIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync([promoOrder]);

        var sut = CreateSut();

        // Act
        var result = await sut.GetMyProfileAsync(20);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.TotalSaved);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdateMyProfileAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateMyProfileAsync_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _consumerProfiles.Setup(c => c.GetByIdWithUserAsync(99, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((ConsumerProfile?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateMyProfileAsync(99, new UpdateConsumerProfileRequest());

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task UpdateMyProfileAsync_WhenFound_UpdatesFieldsAndReturnsRefreshedProfile()
    {
        // Arrange
        var profile = BuildConsumer(20);
        var request = new UpdateConsumerProfileRequest
        {
            FirstName   = "Ana María",
            LastName    = "Gómez",
            PhoneNumber = "351-9998888"
        };

        _consumerProfiles.Setup(c => c.GetByIdWithUserAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        _consumerProfiles.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _orderRepository.Setup(o => o.GetByConsumerIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateMyProfileAsync(20, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Ana María", result.Value.FirstName);
        Assert.Equal("Gómez", result.Value.LastName);
        Assert.Equal("351-9998888", result.Value.PhoneNumber);
        Assert.Equal("Ana María", profile.FirstName);
        _consumerProfiles.Verify(c => c.Update(profile), Times.Once);
        _consumerProfiles.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyOrdersAsync — pure delegation to IOrderService
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyOrdersAsync_DelegatesToOrderService()
    {
        // Arrange
        var expected = Result.Ok<IEnumerable<OrderSummaryResponse>>([new OrderSummaryResponse { Id = 1 }]);
        _orderService.Setup(s => s.GetConsumerOrdersAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var sut = CreateSut();

        // Act
        var result = await sut.GetMyOrdersAsync(20);

        // Assert
        Assert.Same(expected, result);
        _orderService.Verify(s => s.GetConsumerOrdersAsync(20, It.IsAny<CancellationToken>()), Times.Once);
    }
}
