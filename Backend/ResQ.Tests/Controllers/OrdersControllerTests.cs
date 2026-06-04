using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ResQ.API.Common.Errors;
using ResQ.API.Controllers;
using ResQ.API.DTOs.Orders;
using ResQ.API.Services.Orders;

namespace ResQ.Tests.Controllers;

public class OrdersControllerTests
{
    private const int ConsumerProfileId = 7;

    private readonly Mock<IOrderService> _orderService = new();

    private OrdersController CreateSut()
    {
        var claims = new[]
        {
            new Claim("profileId", ConsumerProfileId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, "50")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        return new OrdersController(_orderService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private static OrderSummaryResponse BuildOrderSummary(int id = 1) => new()
    {
        Id                = id,
        ExternalReference = $"ext-{id}",
        MerchantName      = "La Panadería",
        TotalAmount       = 500m,
        OrderStatus       = "Paid",
        PickupCode        = "ABC123",
        CreatedAt         = DateTime.UtcNow,
        Items             = []
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateOrder
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateOrder_WhenServiceReturnsOk_Returns201WithOrderCreatedResponse()
    {
        // Arrange
        var response = new OrderCreatedResponse
        {
            OrderId        = 1,
            MpPreferenceId = "PREF123",
            MpCheckoutUrl  = "https://sandbox.mp.com/checkout"
        };
        _orderService.Setup(s => s.CreateOrderAsync(ConsumerProfileId, It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok(response));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.CreateOrder(new CreateOrderRequest { ProductId = 1, Quantity = 1 }, CancellationToken.None);

        // Assert
        var result = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(201, result.StatusCode);
        var returned = Assert.IsType<OrderCreatedResponse>(result.Value);
        Assert.Equal("PREF123", returned.MpPreferenceId);
    }

    [Fact]
    public async Task CreateOrder_WhenProductNotFound_Returns404()
    {
        // Arrange
        _orderService.Setup(s => s.CreateOrderAsync(ConsumerProfileId, It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail(new NotFoundError("Producto no encontrado.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.CreateOrder(new CreateOrderRequest { ProductId = 99, Quantity = 1 }, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task CreateOrder_WhenInsufficientStock_Returns400()
    {
        // Arrange
        _orderService.Setup(s => s.CreateOrderAsync(ConsumerProfileId, It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail(new BadRequestError("Stock insuficiente.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.CreateOrder(new CreateOrderRequest { ProductId = 1, Quantity = 100 }, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task CreateOrder_PassesConsumerProfileIdFromClaims()
    {
        // Arrange
        _orderService.Setup(s => s.CreateOrderAsync(ConsumerProfileId, It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok(new OrderCreatedResponse()));

        var sut = CreateSut();

        // Act
        await sut.CreateOrder(new CreateOrderRequest(), CancellationToken.None);

        // Assert
        _orderService.Verify(s => s.CreateOrderAsync(ConsumerProfileId, It.IsAny<CreateOrderRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMyOrders
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyOrders_WhenServiceReturnsOk_Returns200WithOrders()
    {
        // Arrange
        var orders = new List<OrderSummaryResponse> { BuildOrderSummary(1), BuildOrderSummary(2) };
        _orderService.Setup(s => s.GetConsumerOrdersAsync(ConsumerProfileId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok<IEnumerable<OrderSummaryResponse>>(orders));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMyOrders(CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<OrderSummaryResponse>>(result.Value).ToList();
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetMyOrders_WhenEmpty_Returns200WithEmptyList()
    {
        // Arrange
        _orderService.Setup(s => s.GetConsumerOrdersAsync(ConsumerProfileId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok<IEnumerable<OrderSummaryResponse>>([]));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetMyOrders(CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var items = Assert.IsAssignableFrom<IEnumerable<OrderSummaryResponse>>(result.Value).ToList();
        Assert.Empty(items);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetOrderById
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetOrderById_WhenOrderNotFound_Returns404()
    {
        // Arrange
        _orderService.Setup(s => s.GetOrderByIdAsync(99, ConsumerProfileId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail(new NotFoundError("Orden no encontrada.")));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetOrderById(99, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetOrderById_WhenOrderFound_Returns200WithOrder()
    {
        // Arrange
        var order = BuildOrderSummary(1);
        _orderService.Setup(s => s.GetOrderByIdAsync(1, ConsumerProfileId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok(order));

        var sut = CreateSut();

        // Act
        var actionResult = await sut.GetOrderById(1, CancellationToken.None);

        // Assert
        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returned = Assert.IsType<OrderSummaryResponse>(result.Value);
        Assert.Equal(1, returned.Id);
        Assert.Equal("La Panadería", returned.MerchantName);
    }

    [Fact]
    public async Task GetOrderById_PassesCorrectIdsToService()
    {
        // Arrange
        _orderService.Setup(s => s.GetOrderByIdAsync(5, ConsumerProfileId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok(BuildOrderSummary(5)));

        var sut = CreateSut();

        // Act
        await sut.GetOrderById(5, CancellationToken.None);

        // Assert
        _orderService.Verify(s => s.GetOrderByIdAsync(5, ConsumerProfileId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
