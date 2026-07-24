using System.Text;
using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Shared;
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
using ResQ.API.Services.Notifications;
using Microsoft.Extensions.Logging;

namespace ResQ.API.Services.Orders;

public class OrderService(
    IOrderRepository orders,
    IProductRepository products,
    IMerchantMpCredentialRepository credentialRepo,
    IEncryptionService encryption,
    IMercadoPagoHttpClient mpClient,
    IMercadoPagoOAuthService oauthService,
    IEmailService emailService,
    INotificationService notificationService,
    IOptions<MpSettings> mpOptions,
    IHostEnvironment env,
    ILogger<OrderService> logger) : IOrderService
{
    private readonly MpSettings _mp = mpOptions.Value;
    private const decimal PlatformFeeRate = 0.10m;

    /// <summary>
    /// Argentina has observed a fixed UTC-3 offset with no daylight saving since 2009,
    /// so this lookup is safe to cache once per service instance.
    /// </summary>
    private static readonly TimeZoneInfo ArgentinaTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

    // ─── Internal value object ────────────────────────────────────────────────

    /// <summary>
    /// Holds the three monetary figures derived from a sale price and quantity.
    /// Passed between calculation and order-building steps to avoid repeating arithmetic.
    /// </summary>
    private record OrderAmounts(decimal Total, decimal PlatformFee, decimal MerchantEarnings);

    // ─── Public: Create ───────────────────────────────────────────────────────

    /// <summary>
    /// Orchestrates the full order creation flow: validates the product and merchant
    /// credentials, calculates amounts, persists the order, and registers a Checkout Pro
    /// preference in Mercado Pago so the consumer can proceed to payment.
    /// </summary>
    /// <param name="consumerProfileId">ID of the consumer placing the order.</param>
    /// <param name="request">Contains the product ID and quantity being purchased.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="OrderCreatedResponse"/> with the new order ID, the MP preference ID,
    /// and the Checkout Pro URL to redirect the consumer to.
    /// Fails with <see cref="NotFoundError"/> if the product does not exist,
    /// <see cref="BadRequestError"/> if stock is insufficient, the product is inactive,
    /// or the merchant's MP connection is unavailable.
    /// </returns>
    public async Task<Result<OrderCreatedResponse>> CreateOrderAsync(
        int consumerProfileId, CreateOrderRequest request, CancellationToken ct = default)
    {
        var productResult = await ValidateProductAsync(request.ProductId, request.Quantity, ct);
        if (productResult.IsFailed) return productResult.ToResult<OrderCreatedResponse>();
        var product = productResult.Value;

        var credentialResult = await ValidateAndRefreshCredentialAsync(product.MerchantId, ct);
        if (credentialResult.IsFailed) return credentialResult.ToResult<OrderCreatedResponse>();
        var credential = credentialResult.Value;

        var amounts = CalculateAmounts(product.SalePrice, request.Quantity);
        var order   = BuildOrder(consumerProfileId, product, request.Quantity, amounts);

        await PersistOrderAsync(order, ct);

        var accessToken = encryption.Decrypt(credential.AccessToken);
        var prefResult  = await CreateMpPreferenceAsync(order, product.Name, accessToken, ct);
        if (prefResult.IsFailed) return prefResult.ToResult<OrderCreatedResponse>();

        var (prefId, checkoutUrl) = prefResult.Value;
        await FinalizeOrderAsync(order, prefId, ct);

        return Result.Ok(new OrderCreatedResponse
        {
            OrderId        = order.Id,
            MpPreferenceId = prefId,
            MpCheckoutUrl  = checkoutUrl
        });
    }

    // ─── Public: Read ─────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves all orders placed by a given consumer, ordered by creation date.
    /// Includes merchant name and line-item details for each order.
    /// </summary>
    /// <param name="consumerProfileId">ID of the consumer whose orders are being fetched.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of <see cref="OrderSummaryResponse"/> mapped from the consumer's orders.</returns>
    public async Task<Result<IEnumerable<OrderSummaryResponse>>> GetConsumerOrdersAsync(
        int consumerProfileId, CancellationToken ct = default)
    {
        var result = await orders.GetByConsumerIdAsync(consumerProfileId, ct);
        return Result.Ok(result.Select(MapConsumerOrder));
    }

    /// <summary>
    /// Retrieves a single order by its ID, scoped to a specific consumer.
    /// Used for post-payment polling — the frontend calls this endpoint until
    /// <see cref="OrderStatus.Paid"/> is reflected after the MP webhook is processed.
    /// </summary>
    /// <param name="orderId">ID of the order to retrieve.</param>
    /// <param name="consumerProfileId">ID of the consumer making the request, used to prevent cross-consumer access.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="OrderSummaryResponse"/> if found.
    /// Fails with <see cref="NotFoundError"/> if the order does not exist or does not belong to the consumer.
    /// </returns>
    public async Task<Result<OrderSummaryResponse>> GetOrderByIdAsync(
        int orderId, int consumerProfileId, CancellationToken ct = default)
    {
        var order = await orders.GetByIdForConsumerAsync(orderId, consumerProfileId, ct);
        return order is null
            ? Result.Fail(new NotFoundError("Orden no encontrada."))
            : Result.Ok(MapConsumerOrder(order));
    }

    /// <summary>
    /// Retrieves all orders received by a given merchant, including consumer name
    /// and line-item details. Used to populate the merchant's order dashboard.
    /// </summary>
    /// <param name="merchantProfileId">ID of the merchant whose orders are being fetched.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of <see cref="MerchantOrderSummaryResponse"/> for the merchant's orders.</returns>
    public async Task<Result<IEnumerable<MerchantOrderSummaryResponse>>> GetMerchantOrdersAsync(
        int merchantProfileId, CancellationToken ct = default)
    {
        var result = await orders.GetByMerchantIdAsync(merchantProfileId, ct);
        return Result.Ok(result.Select(MapMerchantOrder));
    }

    // ─── Public: Pickup ───────────────────────────────────────────────────────

    /// <summary>
    /// Confirms that a consumer has physically picked up their order.
    /// The merchant scans or types the 6-character pickup code shown to the consumer
    /// after payment. Transitions the order status from <see cref="OrderStatus.Paid"/>
    /// to <see cref="OrderStatus.PickedUp"/>.
    /// </summary>
    /// <param name="merchantProfileId">ID of the merchant confirming the pickup.</param>
    /// <param name="pickupCode">The alphanumeric pickup code provided by the consumer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Updated <see cref="MerchantOrderSummaryResponse"/> on success.
    /// Fails with <see cref="NotFoundError"/> if no matching order is found,
    /// or <see cref="ConflictError"/> if the order is already picked up, cancelled,
    /// or not yet paid.
    /// </returns>
    public async Task<Result<MerchantOrderSummaryResponse>> ConfirmPickupAsync(
        int merchantProfileId, string pickupCode, CancellationToken ct = default)
    {
        var code = pickupCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code))
            return Result.Fail(new ValidationError("Ingresá un código de retiro."));

        var order = await orders.GetByPickupCodeAsync(merchantProfileId, code, ct);
        if (order is null)
            return Result.Fail(new NotFoundError("No se encontró una orden con ese código de retiro."));

        if (order.OrderStatus == OrderStatus.PickedUp)
            return Result.Fail(new ConflictError("Esta orden ya fue retirada."));
        if (order.OrderStatus == OrderStatus.Cancelled)
            return Result.Fail(new ConflictError("Esta orden fue cancelada."));
        if (order.OrderStatus != OrderStatus.Paid)
            return Result.Fail(new ConflictError("El pago de esta orden aún no fue confirmado."));

        order.OrderStatus = OrderStatus.PickedUp;
        order.UpdatedAt   = DateTime.UtcNow;
        orders.Update(order);
        await orders.SaveChangesAsync(ct);

        await emailService.SendReviewRequestAsync(
            toEmail:      order.Consumer.User.Email,
            consumerName: order.Consumer.FirstName,
            merchantName: order.Merchant.BusinessName,
            orderId:      order.Id,
            ct:           ct);

        return Result.Ok(MapMerchantOrder(order));
    }

    // ─── Public: Cancel ───────────────────────────────────────────────────────

    /// <summary>
    /// Cancels a paid order on behalf of the consumer who placed it: requests a full refund
    /// from Mercado Pago, then — only once MP confirms it — marks the order cancelled, zeroes
    /// its platform fee and merchant earnings, and restores the purchased quantity to stock.
    /// </summary>
    /// <param name="consumerProfileId">The identifier of the consumer profile requesting the cancellation.</param>
    /// <param name="orderId">The identifier of the order to cancel.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The updated <see cref="OrderSummaryResponse"/> on success.
    /// Fails with <see cref="NotFoundError"/> if the order does not exist or does not belong
    /// to the consumer, <see cref="ConflictError"/> if the order is not <c>Paid</c> or the
    /// pickup window has already closed, or <see cref="BadRequestError"/> if Mercado Pago
    /// rejects the refund.
    /// </returns>
    public async Task<Result<OrderSummaryResponse>> CancelOrderAsync(
        int consumerProfileId, int orderId, CancellationToken ct = default)
    {
        var order = await orders.GetByIdForConsumerTrackedAsync(orderId, consumerProfileId, ct);
        if (order is null)
            return Result.Fail(new NotFoundError("Orden no encontrada."));

        if (order.OrderStatus != OrderStatus.Paid)
            return Result.Fail(new ConflictError(order.OrderStatus switch
            {
                OrderStatus.Pending   => "La orden todavía no fue pagada, no hay nada que cancelar.",
                OrderStatus.PickedUp  => "Esta orden ya fue retirada, no se puede cancelar.",
                OrderStatus.Cancelled => "Esta orden ya fue cancelada.",
                _                     => "Esta orden no se puede cancelar."
            }));

        var detail  = order.OrderDetails.First();
        var product = detail.Product;

        // The pickup window has no explicit date on the product — it's assumed to fall on
        // the same calendar day the order was placed. Cancellation stays open until that
        // window closes (PickupTimeEnd), not when it opens, so a purchase made mid-window
        // (or after it starts) still leaves the consumer a chance to back out.
        var cutoff = ToArgentinaTime(order.CreatedAt).Date.Add(product.PickupTimeEnd.ToTimeSpan());
        if (ToArgentinaTime(DateTime.UtcNow) >= cutoff)
            return Result.Fail(new ConflictError("La ventana de retiro ya cerró, no se puede cancelar la orden."));

        var credentialResult = await ValidateAndRefreshCredentialAsync(order.MerchantId, ct);
        if (credentialResult.IsFailed) return credentialResult.ToResult<OrderSummaryResponse>();

        var accessToken   = encryption.Decrypt(credentialResult.Value.AccessToken);
        var refundResult  = await RefundPaymentAsync(order.MpPaymentId!.Value, accessToken, ct);
        if (refundResult.IsFailed) return refundResult.ToResult<OrderSummaryResponse>();

        order.OrderStatus      = OrderStatus.Cancelled;
        order.PlatformFee      = 0m;
        order.MerchantEarnings = 0m;
        order.UpdatedAt        = DateTime.UtcNow;
        orders.Update(order);

        product.StockQuantity += detail.Quantity;
        product.UpdatedAt      = DateTime.UtcNow;
        products.Update(product);

        await orders.SaveChangesAsync(ct);

        await NotifyOrderCancelledByConsumerAsync(order);

        return Result.Ok(MapConsumerOrder(order));
    }

    /// <summary>
    /// Converts a UTC timestamp to Argentina local time (fixed UTC-3, no daylight saving).
    /// </summary>
    /// <param name="utc">The UTC timestamp to convert.</param>
    private static DateTime ToArgentinaTime(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), ArgentinaTimeZone);

    /// <summary>
    /// Requests a full refund of the given Mercado Pago payment, using the merchant's own
    /// access token since the payment was collected into the merchant's marketplace account.
    /// </summary>
    /// <param name="paymentId">The Mercado Pago payment ID to refund.</param>
    /// <param name="accessToken">The merchant's decrypted Mercado Pago access token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result"/> if MP accepted the refund; otherwise a failed result
    /// with a <see cref="BadRequestError"/>.
    /// </returns>
    private async Task<Result> RefundPaymentAsync(long paymentId, string accessToken, CancellationToken ct)
    {
        // An empty body triggers a full refund of the payment's total amount.
        // MP requires a fresh idempotency key per refund attempt on this endpoint.
        var content  = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await mpClient.PostAsync(
            $"/v1/payments/{paymentId}/refunds", content, accessToken, ct, Guid.NewGuid().ToString());

        if (response.IsSuccessStatusCode) return Result.Ok();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        logger.LogError(
            "[MP] Refund failed for payment {PaymentId} ({Status}): {Body}",
            paymentId, (int)response.StatusCode, responseBody);

        return Result.Fail(new BadRequestError(
            "No se pudo procesar el reembolso en Mercado Pago. Intentá nuevamente en unos minutos."));
    }

    /// <summary>
    /// Creates an in-app "order cancelled" notification for the merchant, triggered by the
    /// consumer's own cancellation action. Wrapped defensively so a notification failure
    /// never blocks the cancellation itself.
    /// </summary>
    /// <param name="order">The order that was just cancelled, with its line items loaded.</param>
    private async Task NotifyOrderCancelledByConsumerAsync(Order order)
    {
        try
        {
            var packName = order.OrderDetails.FirstOrDefault()?.Product?.Name ?? $"Orden #{order.Id}";

            await notificationService.CreateAsync(
                order.MerchantId,
                NotificationType.OrderCancelled,
                "Orden cancelada por el consumidor",
                packName,
                order.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Notification] Failed to create OrderCancelled notification for order #{OrderId}", order.Id);
        }
    }

    // ─── CreateOrder steps ────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the requested product exists, is active, and has enough stock
    /// to fulfill the requested quantity.
    /// </summary>
    /// <param name="productId">ID of the product being purchased.</param>
    /// <param name="quantity">Number of units requested by the consumer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="Product"/> entity on success.
    /// Fails with <see cref="NotFoundError"/> if the product does not exist,
    /// or <see cref="BadRequestError"/> if it is inactive or has insufficient stock.
    /// </returns>
    private async Task<Result<Product>> ValidateProductAsync(
        int productId, int quantity, CancellationToken ct)
    {
        var product = await products.GetByIdWithMerchantAsync(productId, ct);

        if (product is null)
            return Result.Fail(new NotFoundError("Producto no encontrado."));
        if (!product.IsActive)
            return Result.Fail(new BadRequestError("El producto no está disponible."));
        if (product.StockQuantity < quantity)
            return Result.Fail(new BadRequestError(
                $"Stock insuficiente. Disponible: {product.StockQuantity}."));

        return Result.Ok(product);
    }

    /// <summary>
    /// Ensures the merchant has an active Mercado Pago credential.
    /// If the access token expires within the next 24 hours, an inline refresh is
    /// attempted before returning the credential so the caller always receives a
    /// fresh, usable token.
    /// </summary>
    /// <param name="merchantId">ID of the merchant profile to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The up-to-date <see cref="MerchantMpCredential"/> on success.
    /// Fails with <see cref="BadRequestError"/> if no active credential exists
    /// or if the inline token refresh fails.
    /// </returns>
    private async Task<Result<MerchantMpCredential>> ValidateAndRefreshCredentialAsync(
        int merchantId, CancellationToken ct)
    {
        var credential = await credentialRepo.GetByMerchantIdAsync(merchantId, ct);

        if (credential is null || !credential.IsActive)
            return Result.Fail(new BadRequestError(
                "El comercio no tiene Mercado Pago vinculado."));

        if (credential.AccessTokenExpiresAt <= DateTime.UtcNow.AddDays(1))
        {
            var refreshResult = await oauthService.RefreshTokensAsync(merchantId, ct);
            if (refreshResult.IsFailed)
                return Result.Fail(new BadRequestError(
                    "No se pudo renovar el token de MP del comercio."));

            credential = await credentialRepo.GetByMerchantIdAsync(merchantId, ct);
        }

        return Result.Ok(credential!);
    }

    /// <summary>
    /// Computes the three monetary figures for an order: total sale amount,
    /// platform commission (10%), and the net earnings for the merchant.
    /// The platform fee is rounded to two decimal places.
    /// </summary>
    /// <param name="salePrice">Unit sale price of the product.</param>
    /// <param name="quantity">Number of units being purchased.</param>
    /// <returns>An <see cref="OrderAmounts"/> record with Total, PlatformFee and MerchantEarnings.</returns>
    private static OrderAmounts CalculateAmounts(decimal salePrice, int quantity)
    {
        var total = salePrice * quantity;
        var fee   = Math.Round(total * PlatformFeeRate, 2);
        return new OrderAmounts(total, fee, total - fee);
    }

    /// <summary>
    /// Constructs a new <see cref="Order"/> entity with its associated <see cref="OrderDetail"/>.
    /// The unit price is snapshot from the product's current sale price so that future
    /// price changes do not affect historical order records.
    /// Generates a unique <c>ExternalReference</c> (UUID) used to correlate the order
    /// with the Mercado Pago payment webhook, and a random 6-character pickup code.
    /// </summary>
    /// <param name="consumerProfileId">ID of the consumer placing the order.</param>
    /// <param name="product">The product being purchased, including its merchant association.</param>
    /// <param name="quantity">Number of units ordered.</param>
    /// <param name="amounts">Pre-calculated monetary figures for this order.</param>
    /// <returns>A fully initialised <see cref="Order"/> ready to be persisted.</returns>
    private static Order BuildOrder(
        int consumerProfileId, Product product, int quantity, OrderAmounts amounts) => new()
    {
        ConsumerId        = consumerProfileId,
        MerchantId        = product.MerchantId,
        TotalAmount       = amounts.Total,
        PlatformFee       = amounts.PlatformFee,
        MerchantEarnings  = amounts.MerchantEarnings,
        ExternalReference = Guid.NewGuid().ToString(),
        OrderStatus       = OrderStatus.Pending,
        PickupCode        = GeneratePickupCode(),
        CreatedAt         = DateTime.UtcNow,
        OrderDetails      =
        [
            new OrderDetail
            {
                ProductId = product.Id,
                Quantity  = quantity,
                UnitPrice = product.SalePrice,
                CreatedAt = DateTime.UtcNow
            }
        ]
    };

    /// <summary>
    /// Persists the new order in <see cref="OrderStatus.Pending"/> status.
    /// </summary>
    /// <remarks>
    /// Deliberately does NOT touch <see cref="Product.StockQuantity"/> — stock must only be
    /// decremented once Mercado Pago confirms the payment as approved (see
    /// <c>MpWebhookProcessorService.ProcessPaymentAsync</c>). If the consumer abandons checkout
    /// or the payment is rejected, the order simply stays Pending/Cancelled and no stock was
    /// ever committed, so it remains available for other consumers to buy.
    /// </remarks>
    /// <param name="order">The order entity to insert, including its OrderDetail collection.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task PersistOrderAsync(Order order, CancellationToken ct)
    {
        await orders.AddAsync(order, ct);
        await orders.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Stores the Mercado Pago preference ID on the order after the preference has been
    /// successfully created, then persists the change.
    /// This is a second save after <see cref="PersistOrderAsync"/> because the preference
    /// ID is only available once MP responds — the order must already exist in the DB
    /// to have a valid primary key before calling the MP API.
    /// </summary>
    /// <param name="order">The persisted order to update.</param>
    /// <param name="prefId">The preference ID returned by Mercado Pago.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task FinalizeOrderAsync(Order order, string prefId, CancellationToken ct)
    {
        order.MpPreferenceId = prefId;
        order.UpdatedAt      = DateTime.UtcNow;
        orders.Update(order);
        await orders.SaveChangesAsync(ct);
    }

    // ─── MP preference ────────────────────────────────────────────────────────

    /// <summary>
    /// Calls the Mercado Pago Checkout Pro API to create a payment preference for the order.
    /// Sets the <c>marketplace_fee</c> so the platform commission is automatically split
    /// at payment time. Includes <c>back_urls</c> derived from the configured redirect URI
    /// so MP can redirect the consumer back to ResQ after payment.
    /// Returns the sandbox checkout URL in non-production environments and the live URL in production.
    /// </summary>
    /// <param name="order">The order for which the preference is being created.</param>
    /// <param name="productName">Human-readable product name shown to the consumer in the MP checkout UI.</param>
    /// <param name="accessToken">Decrypted OAuth access token belonging to the merchant's MP account.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A tuple of <c>(PreferenceId, CheckoutUrl)</c> on success.
    /// Fails with <see cref="BadRequestError"/> if MP rejects the request or returns an unparseable response.
    /// </returns>
    private async Task<Result<(string PreferenceId, string CheckoutUrl)>> CreateMpPreferenceAsync(
        Order order, string productName, string accessToken, CancellationToken ct)
    {
        var frontendBase = _mp.FrontendBaseUrl.TrimEnd('/');

        var body = new MpPreferenceRequest(
            Items: [new MpItem(
                Id:          order.Id.ToString(),
                Title:       productName,
                Description: $"Pack sorpresa ResQ — orden #{order.Id}",
                CategoryId:  "food",
                Quantity:    1,
                CurrencyId:  "ARS",
                UnitPrice:   order.TotalAmount)],
            // marketplace_fee must be > 0 even in dev/sandbox: MP requires a positive
            // commission to activate the marketplace split flow. Passing 0 makes MP
            // treat the preference as a regular (non-marketplace) payment and then
            // reject the checkout because the merchant has no direct collection rights.
            MarketplaceFee:    order.PlatformFee,
            ExternalReference: order.ExternalReference,
            BackUrls: new MpBackUrls(
                Success: $"{frontendBase}/pago/exitoso?orderId={order.Id}",
                Failure: $"{frontendBase}/pago/fallido?orderId={order.Id}",
                Pending: $"{frontendBase}/pago/pendiente?orderId={order.Id}"),
            AutoReturn:          env.IsProduction() ? "approved" : null,
            NotificationUrl:     _mp.NotificationUrl,
            StatementDescriptor: "RESQ",
            BinaryMode:          false,            // Allow "pending" status (required for cash payments)
            // In non-production environments, exclude cash-based payment types
            // (ticket vouchers, ATM transfers) since they cannot be completed
            // synchronously and are not useful for end-to-end testing.
            PaymentMethods: env.IsProduction() ? null : new MpPaymentMethods(
                ExcludedPaymentTypes: [new MpPaymentMethodId("ticket"), new MpPaymentMethodId("atm")],
                Installments:         12)
        );

        var requestJson = JsonSerializer.Serialize(body);
        logger.LogInformation("[MP] Sending preference request: {Body}", requestJson);

        var content  = new StringContent(requestJson, Encoding.UTF8, "application/json");
        var response = await mpClient.PostAsync("/checkout/preferences", content, accessToken, ct);

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        logger.LogInformation("[MP] Preference response ({Status}): {Body}", (int)response.StatusCode, responseBody);

        if (!response.IsSuccessStatusCode)
            return Result.Fail(new BadRequestError($"Error al crear preferencia en MP: {responseBody}"));

        var pref = JsonSerializer.Deserialize<MpPreferenceApiResponse>(responseBody);

        if (pref is null)
            return Result.Fail(new BadRequestError("Respuesta inválida de MP al crear preferencia."));

        // Always use init_point. MP determines test vs. live mode from the credentials,
        // not the checkout URL, so init_point works for both environments when the
        // underlying access token is correctly issued. The legacy sandbox_init_point
        // (sandbox.mercadopago.com.ar) is deprecated.
        return Result.Ok((pref.Id, pref.InitPoint));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a random 6-character alphanumeric code (uppercase letters and digits)
    /// used as the pickup verification code for an order.
    /// The consumer presents this code to the merchant when collecting their pack.
    /// </summary>
    /// <returns>A 6-character uppercase alphanumeric string, e.g. <c>"A3X7K2"</c>.</returns>
    private static string GeneratePickupCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }

    /// <summary>
    /// Maps an <see cref="Order"/> entity to a <see cref="OrderSummaryResponse"/> DTO
    /// for consumer-facing endpoints. Includes merchant name and full item breakdown.
    /// </summary>
    /// <param name="o">The order entity with its navigation properties loaded.</param>
    /// <returns>A <see cref="OrderSummaryResponse"/> safe to expose through the API.</returns>
    private static OrderSummaryResponse MapConsumerOrder(Order o) => new()
    {
        Id                = o.Id,
        ExternalReference = o.ExternalReference,
        MerchantName      = o.Merchant.BusinessName,
        TotalAmount       = o.TotalAmount,
        OrderStatus       = o.OrderStatus.ToString(),
        PickupCode        = o.PickupCode,
        CreatedAt         = o.CreatedAt,
        HasReview         = o.Review is not null,
        Items             = o.OrderDetails.Select(od => new OrderDetailItemResponse
        {
            ProductName = od.Product.Name,
            Quantity    = od.Quantity,
            UnitPrice   = od.UnitPrice
        }).ToList()
    };

    /// <summary>
    /// Maps an <see cref="Order"/> entity to a <see cref="MerchantOrderSummaryResponse"/> DTO
    /// for merchant-facing endpoints. Includes consumer full name and full item breakdown.
    /// </summary>
    /// <param name="o">The order entity with its navigation properties loaded.</param>
    /// <returns>A <see cref="MerchantOrderSummaryResponse"/> safe to expose through the API.</returns>
    private static MerchantOrderSummaryResponse MapMerchantOrder(Order o) => new()
    {
        Id           = o.Id,
        ConsumerName = $"{o.Consumer.FirstName} {o.Consumer.LastName}",
        TotalAmount  = o.TotalAmount,
        OrderStatus  = o.OrderStatus.ToString(),
        PickupCode   = o.PickupCode,
        CreatedAt    = o.CreatedAt,
        Items        = o.OrderDetails.Select(od => new OrderDetailItemResponse
        {
            ProductName = od.Product.Name,
            Quantity    = od.Quantity,
            UnitPrice   = od.UnitPrice
        }).ToList()
    };
}
