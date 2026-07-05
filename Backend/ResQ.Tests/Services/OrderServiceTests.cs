using System.Net;
using System.Text;
using FluentResults;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ResQ.API.DTOs.Orders;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Models.Orders;
using ResQ.API.Models.Settings;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Repositories.MercadoPago;
using ResQ.API.Repositories.Orders;
using ResQ.API.Services.Email;
using ResQ.API.Services.Encryption;
using ResQ.API.Services.MercadoPago;
using ResQ.API.Services.Orders;

namespace ResQ.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IMerchantMpCredentialRepository> _credentials = new();
    private readonly Mock<IEncryptionService> _encryption = new();
    private readonly Mock<IMercadoPagoHttpClient> _mpClient = new();
    private readonly Mock<IMercadoPagoOAuthService> _oauthService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IHostEnvironment> _env = new();

    private readonly IOptions<MpSettings> _mpOptions = Options.Create(new MpSettings
    {
        RedirectUri     = "https://frontend.resq.com/auth/callback",
        NotificationUrl = "https://api.resq.com/webhooks/mercadopago"
    });

    private OrderService CreateSut() => new(
        _orders.Object,
        _products.Object,
        _credentials.Object,
        _encryption.Object,
        _mpClient.Object,
        _oauthService.Object,
        _emailService.Object,
        _mpOptions,
        _env.Object,
        NullLogger<OrderService>.Instance);

    private static Product BuildActiveProduct(int id = 1, int merchantId = 10, int stock = 5) => new()
    {
        Id            = id,
        MerchantId    = merchantId,
        Name          = "Pack Sorpresa",
        SalePrice     = 500m,
        StockQuantity = stock,
        IsActive      = true,
        Merchant      = new MerchantProfile { Id = merchantId, BusinessName = "La Panadería" }
    };

    private static MerchantMpCredential BuildActiveCredential(int merchantId = 10) => new()
    {
        MerchantId           = merchantId,
        AccessToken          = "encrypted-token",
        RefreshToken         = "encrypted-refresh",
        AccessTokenExpiresAt = DateTime.UtcNow.AddDays(30),
        IsActive             = true
    };

    private static HttpResponseMessage BuildMpSuccessResponse(
        string prefId = "PREF123", string initPoint = "https://mp.com/pay") =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$$"""{"id":"{{{prefId}}}","init_point":"{{{initPoint}}}","sandbox_init_point":"https://sandbox.mp.com/pay"}""",
                Encoding.UTF8,
                "application/json")
        };

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateOrderAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateOrderAsync_WhenProductNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _products.Setup(p => p.GetByIdWithMerchantAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Product?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateOrderAsync(1, new CreateOrderRequest { ProductId = 99, Quantity = 1 });

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrado", result.Errors[0].Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenProductInactive_ReturnsBadRequestError()
    {
        // Arrange
        var product = BuildActiveProduct();
        product.IsActive = false;
        _products.Setup(p => p.GetByIdWithMerchantAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateOrderAsync(1, new CreateOrderRequest { ProductId = 1, Quantity = 1 });

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no está disponible", result.Errors[0].Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenInsufficientStock_ReturnsBadRequestError()
    {
        // Arrange
        var product = BuildActiveProduct(stock: 2);
        _products.Setup(p => p.GetByIdWithMerchantAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateOrderAsync(1, new CreateOrderRequest { ProductId = 1, Quantity = 5 });

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Stock insuficiente", result.Errors[0].Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenMerchantHasNoCredential_ReturnsBadRequestError()
    {
        // Arrange
        var product = BuildActiveProduct();
        _products.Setup(p => p.GetByIdWithMerchantAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        _credentials.Setup(c => c.GetByMerchantIdAsync(product.MerchantId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((MerchantMpCredential?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateOrderAsync(1, new CreateOrderRequest { ProductId = 1, Quantity = 1 });

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Mercado Pago", result.Errors[0].Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenCredentialInactive_ReturnsBadRequestError()
    {
        // Arrange
        var product = BuildActiveProduct();
        var credential = BuildActiveCredential();
        credential.IsActive = false;

        _products.Setup(p => p.GetByIdWithMerchantAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        _credentials.Setup(c => c.GetByMerchantIdAsync(product.MerchantId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(credential);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateOrderAsync(1, new CreateOrderRequest { ProductId = 1, Quantity = 1 });

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Mercado Pago", result.Errors[0].Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenTokenExpiringSoon_RefreshesTokenBeforeCreatingOrder()
    {
        // Arrange
        var product = BuildActiveProduct();
        var expiredCredential = BuildActiveCredential();
        expiredCredential.AccessTokenExpiresAt = DateTime.UtcNow; // will trigger refresh

        var freshCredential = BuildActiveCredential();
        freshCredential.AccessToken = "encrypted-fresh-token";

        _products.Setup(p => p.GetByIdWithMerchantAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        _credentials.SetupSequence(c => c.GetByMerchantIdAsync(product.MerchantId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expiredCredential)
                    .ReturnsAsync(freshCredential);
        _oauthService.Setup(o => o.RefreshTokensAsync(product.MerchantId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Ok());
        _encryption.Setup(e => e.Decrypt("encrypted-fresh-token")).Returns("plain-fresh-token");
        _orders.Setup(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);
        _orders.Setup(o => o.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mpClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(BuildMpSuccessResponse());
        _env.Setup(e => e.EnvironmentName).Returns("Development");

        var sut = CreateSut();

        // Act
        var result = await sut.CreateOrderAsync(1, new CreateOrderRequest { ProductId = 1, Quantity = 1 });

        // Assert
        _oauthService.Verify(o => o.RefreshTokensAsync(product.MerchantId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenTokenRefreshFails_ReturnsBadRequestError()
    {
        // Arrange
        var product = BuildActiveProduct();
        var expiredCredential = BuildActiveCredential();
        expiredCredential.AccessTokenExpiresAt = DateTime.UtcNow;

        _products.Setup(p => p.GetByIdWithMerchantAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        _credentials.Setup(c => c.GetByMerchantIdAsync(product.MerchantId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expiredCredential);
        _oauthService.Setup(o => o.RefreshTokensAsync(product.MerchantId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result.Fail("Token refresh failed"));

        var sut = CreateSut();

        // Act
        var result = await sut.CreateOrderAsync(1, new CreateOrderRequest { ProductId = 1, Quantity = 1 });

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("renovar el token", result.Errors[0].Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenMpPreferenceFails_ReturnsBadRequestError()
    {
        // Arrange
        var product = BuildActiveProduct();
        var credential = BuildActiveCredential();

        _products.Setup(p => p.GetByIdWithMerchantAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        _credentials.Setup(c => c.GetByMerchantIdAsync(product.MerchantId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(credential);
        _encryption.Setup(e => e.Decrypt(credential.AccessToken)).Returns("plain-token");
        _orders.Setup(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);
        _orders.Setup(o => o.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mpClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
                 {
                     Content = new StringContent("unauthorized", Encoding.UTF8, "application/json")
                 });
        _env.Setup(e => e.EnvironmentName).Returns("Development");

        var sut = CreateSut();

        // Act
        var result = await sut.CreateOrderAsync(1, new CreateOrderRequest { ProductId = 1, Quantity = 1 });

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Error al crear preferencia", result.Errors[0].Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenAllValid_CreatesOrderDeductsStockAndReturnsMpCheckoutUrl()
    {
        // Arrange
        const int consumerProfileId = 5;
        var product = BuildActiveProduct(stock: 10);
        var credential = BuildActiveCredential();

        _products.Setup(p => p.GetByIdWithMerchantAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(product);
        _credentials.Setup(c => c.GetByMerchantIdAsync(product.MerchantId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(credential);
        _encryption.Setup(e => e.Decrypt(credential.AccessToken)).Returns("plain-token");
        _orders.Setup(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);
        _orders.Setup(o => o.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mpClient.Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(BuildMpSuccessResponse("PREF999", "https://mp.com/pref999"));
        _env.Setup(e => e.EnvironmentName).Returns("Development");

        var sut = CreateSut();

        // Act
        var result = await sut.CreateOrderAsync(consumerProfileId, new CreateOrderRequest { ProductId = 1, Quantity = 2 });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("PREF999", result.Value.MpPreferenceId);
        Assert.Equal("https://mp.com/pref999", result.Value.MpCheckoutUrl);
        Assert.Equal(8, product.StockQuantity); // 10 - 2
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetConsumerOrdersAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetConsumerOrdersAsync_ReturnsAllOrdersMappedToResponse()
    {
        // Arrange
        var orders = new List<Order>
        {
            new()
            {
                Id                = 1,
                ConsumerId        = 5,
                ExternalReference = "ext-ref-1",
                TotalAmount       = 500m,
                OrderStatus       = OrderStatus.Paid,
                PickupCode        = "ABC123",
                CreatedAt         = DateTime.UtcNow,
                Merchant          = new MerchantProfile { BusinessName = "La Panadería" },
                OrderDetails      = []
            }
        };

        _orders.Setup(o => o.GetByConsumerIdAsync(5, It.IsAny<CancellationToken>()))
               .ReturnsAsync(orders);

        var sut = CreateSut();

        // Act
        var result = await sut.GetConsumerOrdersAsync(5);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value.ToList();
        Assert.Single(items);
        Assert.Equal("La Panadería", items[0].MerchantName);
        Assert.Equal("Paid", items[0].OrderStatus);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetOrderByIdAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetOrderByIdAsync_WhenOrderNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _orders.Setup(o => o.GetByIdForConsumerAsync(99, 5, It.IsAny<CancellationToken>()))
               .ReturnsAsync((Order?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetOrderByIdAsync(99, 5);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("no encontrada", result.Errors[0].Message);
    }

    [Fact]
    public async Task GetOrderByIdAsync_WhenOrderFound_ReturnsOrderSummaryResponse()
    {
        // Arrange
        var order = new Order
        {
            Id                = 1,
            ConsumerId        = 5,
            ExternalReference = "ext-ref",
            TotalAmount       = 800m,
            OrderStatus       = OrderStatus.Pending,
            PickupCode        = "XYZ789",
            CreatedAt         = DateTime.UtcNow,
            Merchant          = new MerchantProfile { BusinessName = "Restaurante ABC" },
            OrderDetails      = []
        };

        _orders.Setup(o => o.GetByIdForConsumerAsync(1, 5, It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);

        var sut = CreateSut();

        // Act
        var result = await sut.GetOrderByIdAsync(1, 5);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("ext-ref", result.Value.ExternalReference);
        Assert.Equal("Restaurante ABC", result.Value.MerchantName);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetMerchantOrdersAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMerchantOrdersAsync_ReturnsAllOrdersMappedToResponse()
    {
        // Arrange
        var orders = new List<Order>
        {
            new()
            {
                Id           = 1,
                MerchantId   = 10,
                TotalAmount  = 600m,
                OrderStatus  = OrderStatus.Paid,
                PickupCode   = "PICK01",
                CreatedAt    = DateTime.UtcNow,
                Consumer     = new ConsumerProfile { FirstName = "Juan", LastName = "Pérez" },
                OrderDetails = []
            }
        };

        _orders.Setup(o => o.GetByMerchantIdAsync(10, It.IsAny<CancellationToken>()))
               .ReturnsAsync(orders);

        var sut = CreateSut();

        // Act
        var result = await sut.GetMerchantOrdersAsync(10);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value.ToList();
        Assert.Single(items);
        Assert.Equal("Juan Pérez", items[0].ConsumerName);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ConfirmPickupAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ConfirmPickupAsync_WhenCodeEmptyOrWhitespace_ReturnsValidationError(string? code)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.ConfirmPickupAsync(10, code!);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("código de retiro", result.Errors[0].Message);
    }

    [Fact]
    public async Task ConfirmPickupAsync_WhenOrderNotFound_ReturnsNotFoundError()
    {
        // Arrange
        _orders.Setup(o => o.GetByPickupCodeAsync(10, "ABC123", It.IsAny<CancellationToken>()))
               .ReturnsAsync((Order?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.ConfirmPickupAsync(10, "ABC123");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("No se encontró", result.Errors[0].Message);
    }

    [Fact]
    public async Task ConfirmPickupAsync_WhenOrderAlreadyPickedUp_ReturnsConflictError()
    {
        // Arrange
        var order = BuildOrderForPickup(OrderStatus.PickedUp);
        _orders.Setup(o => o.GetByPickupCodeAsync(10, "ABC123", It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);

        var sut = CreateSut();

        // Act
        var result = await sut.ConfirmPickupAsync(10, "ABC123");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("ya fue retirada", result.Errors[0].Message);
    }

    [Fact]
    public async Task ConfirmPickupAsync_WhenOrderCancelled_ReturnsConflictError()
    {
        // Arrange
        var order = BuildOrderForPickup(OrderStatus.Cancelled);
        _orders.Setup(o => o.GetByPickupCodeAsync(10, "ABC123", It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);

        var sut = CreateSut();

        // Act
        var result = await sut.ConfirmPickupAsync(10, "ABC123");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("cancelada", result.Errors[0].Message);
    }

    [Fact]
    public async Task ConfirmPickupAsync_WhenOrderStillPending_ReturnsConflictError()
    {
        // Arrange
        var order = BuildOrderForPickup(OrderStatus.Pending);
        _orders.Setup(o => o.GetByPickupCodeAsync(10, "ABC123", It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);

        var sut = CreateSut();

        // Act
        var result = await sut.ConfirmPickupAsync(10, "ABC123");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("pago", result.Errors[0].Message);
    }

    [Fact]
    public async Task ConfirmPickupAsync_WhenOrderIsPaid_SetsPickedUpStatusAndReturnsOk()
    {
        // Arrange
        var order = BuildOrderForPickup(OrderStatus.Paid);
        _orders.Setup(o => o.GetByPickupCodeAsync(10, "PAID01", It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);
        _orders.Setup(o => o.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.ConfirmPickupAsync(10, "PAID01");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.PickedUp.ToString(), result.Value.OrderStatus);
        _orders.Verify(o => o.Update(order), Times.Once);
    }

    private static Order BuildOrderForPickup(OrderStatus status) => new()
    {
        Id           = 1,
        MerchantId   = 10,
        TotalAmount  = 500m,
        OrderStatus  = status,
        PickupCode   = status == OrderStatus.Paid ? "PAID01" : "ABC123",
        CreatedAt    = DateTime.UtcNow,
        Consumer     = new ConsumerProfile { FirstName = "Ana", LastName = "López" },
        OrderDetails = []
    };
}
