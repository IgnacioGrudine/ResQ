using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ResQ.API.Models.Catalog;
using ResQ.API.Models.Enums;
using ResQ.API.Models.MercadoPago;
using ResQ.API.Models.Orders;
using ResQ.API.Models.Settings;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Repositories.MercadoPago;
using ResQ.API.Repositories.Orders;
using ResQ.API.Services.MercadoPago;
using ResQ.API.Services.Notifications;

namespace ResQ.Tests.Services;

public class MpWebhookProcessorServiceTests
{
    private readonly Mock<IMercadoPagoHttpClient> _mpClient = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IMpWebhookEventRepository> _webhookEventRepo = new();
    private readonly Mock<INotificationService> _notificationService = new();

    private readonly IOptions<MpSettings> _mpOptions = Options.Create(new MpSettings
    {
        AdminAccessToken = "admin-token"
    });

    private MpWebhookProcessorService CreateSut() => new(
        _mpClient.Object,
        _mpOptions,
        _orderRepo.Object,
        _productRepo.Object,
        _webhookEventRepo.Object,
        _notificationService.Object,
        NullLogger<MpWebhookProcessorService>.Instance);

    private static HttpResponseMessage BuildPaymentResponse(
        long id = 555, string status = "approved", string externalReference = "ext-ref-1", decimal amount = 500m) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"id":{{id}},"status":"{{status}}","external_reference":"{{externalReference}}","transaction_amount":{{amount}}}""",
                Encoding.UTF8,
                "application/json")
        };

    private static Order BuildOrder(OrderStatus status = OrderStatus.Pending, int merchantId = 10) => new()
    {
        Id                = 1,
        MerchantId        = merchantId,
        TotalAmount       = 500m,
        ExternalReference = "ext-ref-1",
        OrderStatus       = status,
        PickupCode        = "ABC123",
        CreatedAt         = DateTime.UtcNow,
        OrderDetails      = []
    };

    private static MpWebhookEvent BuildWebhookEvent(long notificationId = 555) => new()
    {
        Id                = 1,
        MpNotificationId  = notificationId,
        Topic             = "payment",
        MpResourceId      = notificationId,
        ProcessingStatus  = WebhookProcessingStatus.Pending,
        CreatedAt         = DateTime.UtcNow
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // ProcessPaymentAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ProcessPaymentAsync_WhenMpApiCallFails_MarksEventAsError()
    {
        // Arrange
        _mpClient.Setup(c => c.GetAsync("/v1/payments/555", "admin-token", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));
        var ev = BuildWebhookEvent();
        _webhookEventRepo.Setup(r => r.GetByNotificationIdAsync(555, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _webhookEventRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.ProcessPaymentAsync(555, 555);

        // Assert
        Assert.Equal(WebhookProcessingStatus.Error, ev.ProcessingStatus);
        Assert.Contains("404", ev.LastErrorMessage);
        _orderRepo.Verify(o => o.GetByExternalReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenResponseBodyDeserializesToNull_MarksEventAsError()
    {
        // Arrange
        _mpClient.Setup(c => c.GetAsync("/v1/payments/555", "admin-token", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                 {
                     Content = new StringContent("null", Encoding.UTF8, "application/json")
                 });
        var ev = BuildWebhookEvent();
        _webhookEventRepo.Setup(r => r.GetByNotificationIdAsync(555, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _webhookEventRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.ProcessPaymentAsync(555, 555);

        // Assert
        Assert.Equal(WebhookProcessingStatus.Error, ev.ProcessingStatus);
        Assert.Contains("deserialize", ev.LastErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenStatusIsIntermediate_MarksEventProcessedWithoutTouchingOrder()
    {
        // Arrange
        _mpClient.Setup(c => c.GetAsync("/v1/payments/555", "admin-token", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(BuildPaymentResponse(status: "in_process"));
        var ev = BuildWebhookEvent();
        _webhookEventRepo.Setup(r => r.GetByNotificationIdAsync(555, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _webhookEventRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.ProcessPaymentAsync(555, 555);

        // Assert
        Assert.Equal(WebhookProcessingStatus.Processed, ev.ProcessingStatus);
        _orderRepo.Verify(o => o.GetByExternalReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenOrderNotFound_MarksEventAsError()
    {
        // Arrange
        _mpClient.Setup(c => c.GetAsync("/v1/payments/555", "admin-token", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(BuildPaymentResponse());
        _orderRepo.Setup(o => o.GetByExternalReferenceAsync("ext-ref-1", It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);
        var ev = BuildWebhookEvent();
        _webhookEventRepo.Setup(r => r.GetByNotificationIdAsync(555, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _webhookEventRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.ProcessPaymentAsync(555, 555);

        // Assert
        Assert.Equal(WebhookProcessingStatus.Error, ev.ProcessingStatus);
        Assert.Contains("No order found", ev.LastErrorMessage);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenApprovedAndOrderPending_MarksOrderPaidAndNotifiesMerchant()
    {
        // Arrange
        _mpClient.Setup(c => c.GetAsync("/v1/payments/555", "admin-token", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(BuildPaymentResponse());
        var order = BuildOrder(OrderStatus.Pending);
        _orderRepo.Setup(o => o.GetByExternalReferenceAsync("ext-ref-1", It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepo.Setup(o => o.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var ev = BuildWebhookEvent();
        _webhookEventRepo.Setup(r => r.GetByNotificationIdAsync(555, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _webhookEventRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.ProcessPaymentAsync(555, 555);

        // Assert
        Assert.Equal(OrderStatus.Paid, order.OrderStatus);
        Assert.Equal(555L, order.MpPaymentId);
        _orderRepo.Verify(o => o.Update(order), Times.Once);
        _orderRepo.Verify(o => o.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationService.Verify(n => n.CreateAsync(
            order.MerchantId, NotificationType.OrderPaid, It.IsAny<string>(), It.IsAny<string>(), order.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(WebhookProcessingStatus.Processed, ev.ProcessingStatus);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenApprovedAndOrderPending_DecrementsStockForEachLineItem()
    {
        // Arrange — stock must only ever be committed once payment is confirmed approved,
        // never at order creation (see OrderService.PersistOrderAsync).
        _mpClient.Setup(c => c.GetAsync("/v1/payments/555", "admin-token", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(BuildPaymentResponse());

        var product = new Product { Id = 1, MerchantId = 10, Name = "Pack Sorpresa", StockQuantity = 5 };
        var order   = BuildOrder(OrderStatus.Pending);
        order.OrderDetails = [new OrderDetail { ProductId = product.Id, Quantity = 2, UnitPrice = 400m, Product = product }];

        _orderRepo.Setup(o => o.GetByExternalReferenceAsync("ext-ref-1", It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepo.Setup(o => o.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var ev = BuildWebhookEvent();
        _webhookEventRepo.Setup(r => r.GetByNotificationIdAsync(555, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _webhookEventRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.ProcessPaymentAsync(555, 555);

        // Assert
        Assert.Equal(3, product.StockQuantity); // 5 - 2
        _productRepo.Verify(p => p.Update(product), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenApprovedAndOrderAlreadyPaid_DoesNotReprocessOrNotifyAgain()
    {
        // Arrange
        _mpClient.Setup(c => c.GetAsync("/v1/payments/555", "admin-token", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(BuildPaymentResponse());
        var order = BuildOrder(OrderStatus.Paid);
        _orderRepo.Setup(o => o.GetByExternalReferenceAsync("ext-ref-1", It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var ev = BuildWebhookEvent();
        _webhookEventRepo.Setup(r => r.GetByNotificationIdAsync(555, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _webhookEventRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.ProcessPaymentAsync(555, 555);

        // Assert
        _orderRepo.Verify(o => o.Update(It.IsAny<Order>()), Times.Never);
        _orderRepo.Verify(o => o.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notificationService.Verify(n => n.CreateAsync(
            It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal(WebhookProcessingStatus.Processed, ev.ProcessingStatus);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenRejectedAndOrderPending_MarksOrderCancelledAndNotifiesMerchant()
    {
        // Arrange
        _mpClient.Setup(c => c.GetAsync("/v1/payments/555", "admin-token", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(BuildPaymentResponse(status: "rejected"));
        var order = BuildOrder(OrderStatus.Pending);
        _orderRepo.Setup(o => o.GetByExternalReferenceAsync("ext-ref-1", It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepo.Setup(o => o.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var ev = BuildWebhookEvent();
        _webhookEventRepo.Setup(r => r.GetByNotificationIdAsync(555, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _webhookEventRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.ProcessPaymentAsync(555, 555);

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.OrderStatus);
        _notificationService.Verify(n => n.CreateAsync(
            order.MerchantId, NotificationType.OrderCancelled, It.IsAny<string>(), It.IsAny<string>(), order.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenNotificationServiceThrows_OrderIsStillUpdatedAndEventMarkedProcessed()
    {
        // Arrange
        _mpClient.Setup(c => c.GetAsync("/v1/payments/555", "admin-token", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(BuildPaymentResponse());
        var order = BuildOrder(OrderStatus.Pending);
        _orderRepo.Setup(o => o.GetByExternalReferenceAsync("ext-ref-1", It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepo.Setup(o => o.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _notificationService.Setup(n => n.CreateAsync(
                It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("notification service down"));
        var ev = BuildWebhookEvent();
        _webhookEventRepo.Setup(r => r.GetByNotificationIdAsync(555, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _webhookEventRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.ProcessPaymentAsync(555, 555);

        // Assert
        Assert.Equal(OrderStatus.Paid, order.OrderStatus);
        Assert.Equal(WebhookProcessingStatus.Processed, ev.ProcessingStatus);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenOrderRepositoryThrows_MarksEventAsErrorWithExceptionMessage()
    {
        // Arrange
        _mpClient.Setup(c => c.GetAsync("/v1/payments/555", "admin-token", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(BuildPaymentResponse());
        _orderRepo.Setup(o => o.GetByExternalReferenceAsync("ext-ref-1", It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("DB unavailable"));
        var ev = BuildWebhookEvent();
        _webhookEventRepo.Setup(r => r.GetByNotificationIdAsync(555, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _webhookEventRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        await sut.ProcessPaymentAsync(555, 555);

        // Assert
        Assert.Equal(WebhookProcessingStatus.Error, ev.ProcessingStatus);
        Assert.Equal("DB unavailable", ev.LastErrorMessage);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenWebhookEventRowNotFound_CompletesWithoutThrowing()
    {
        // Arrange
        _mpClient.Setup(c => c.GetAsync("/v1/payments/555", "admin-token", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));
        _webhookEventRepo.Setup(r => r.GetByNotificationIdAsync(555, It.IsAny<CancellationToken>())).ReturnsAsync((MpWebhookEvent?)null);

        var sut = CreateSut();

        // Act
        var exception = await Record.ExceptionAsync(() => sut.ProcessPaymentAsync(555, 555));

        // Assert
        Assert.Null(exception);
        _webhookEventRepo.Verify(r => r.Update(It.IsAny<MpWebhookEvent>()), Times.Never);
        _webhookEventRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
