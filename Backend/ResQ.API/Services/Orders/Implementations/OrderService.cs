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
using ResQ.API.Services.Encryption;
using ResQ.API.Services.MercadoPago;

namespace ResQ.API.Services.Orders;

public class OrderService(
    IOrderRepository orders,
    IProductRepository products,
    IMerchantMpCredentialRepository credentialRepo,
    IEncryptionService encryption,
    IMercadoPagoHttpClient mpClient,
    IMercadoPagoOAuthService oauthService,
    IOptions<MpSettings> mpOptions,
    IHostEnvironment env) : IOrderService
{
    private readonly MpSettings _mp = mpOptions.Value;
    private const decimal PlatformFeeRate = 0.10m;

    // ─── Internal value object ────────────────────────────────────────────────

    private record OrderAmounts(decimal Total, decimal PlatformFee, decimal MerchantEarnings);

    // ─── Public: Create ───────────────────────────────────────────────────────

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

        await PersistOrderAsync(order, product, request.Quantity, ct);

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

    public async Task<Result<IEnumerable<OrderSummaryResponse>>> GetConsumerOrdersAsync(
        int consumerProfileId, CancellationToken ct = default)
    {
        var result = await orders.GetByConsumerIdAsync(consumerProfileId, ct);
        return Result.Ok(result.Select(MapConsumerOrder));
    }

    public async Task<Result<OrderSummaryResponse>> GetOrderByIdAsync(
        int orderId, int consumerProfileId, CancellationToken ct = default)
    {
        var order = await orders.GetByIdForConsumerAsync(orderId, consumerProfileId, ct);
        return order is null
            ? Result.Fail(new NotFoundError("Orden no encontrada."))
            : Result.Ok(MapConsumerOrder(order));
    }

    public async Task<Result<IEnumerable<MerchantOrderSummaryResponse>>> GetMerchantOrdersAsync(
        int merchantProfileId, CancellationToken ct = default)
    {
        var result = await orders.GetByMerchantIdAsync(merchantProfileId, ct);
        return Result.Ok(result.Select(MapMerchantOrder));
    }

    // ─── Public: Pickup ───────────────────────────────────────────────────────

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

        return Result.Ok(MapMerchantOrder(order));
    }

    // ─── CreateOrder steps ────────────────────────────────────────────────────

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

    private static OrderAmounts CalculateAmounts(decimal salePrice, int quantity)
    {
        var total = salePrice * quantity;
        var fee   = Math.Round(total * PlatformFeeRate, 2);
        return new OrderAmounts(total, fee, total - fee);
    }

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

    private async Task PersistOrderAsync(
        Order order, Product product, int quantity, CancellationToken ct)
    {
        await orders.AddAsync(order, ct);

        product.StockQuantity -= quantity;
        product.UpdatedAt      = DateTime.UtcNow;
        products.Update(product);

        await orders.SaveChangesAsync(ct);
    }

    private async Task FinalizeOrderAsync(Order order, string prefId, CancellationToken ct)
    {
        order.MpPreferenceId = prefId;
        order.UpdatedAt      = DateTime.UtcNow;
        orders.Update(order);
        await orders.SaveChangesAsync(ct);
    }

    // ─── MP preference ────────────────────────────────────────────────────────

    private async Task<Result<(string PreferenceId, string CheckoutUrl)>> CreateMpPreferenceAsync(
        Order order, string productName, string accessToken, CancellationToken ct)
    {
        var frontendBase = new Uri(_mp.RedirectUri).GetLeftPart(UriPartial.Authority);

        var body = new MpPreferenceRequest(
            Items:             [new MpItem(productName, 1, "ARS", order.TotalAmount)],
            MarketplaceFee:    order.PlatformFee,
            ExternalReference: order.ExternalReference,
            BackUrls: new MpBackUrls(
                Success: $"{frontendBase}/pago/exitoso?orderId={order.Id}",
                Failure: $"{frontendBase}/pago/fallido?orderId={order.Id}",
                Pending: $"{frontendBase}/pago/pendiente?orderId={order.Id}"),
            AutoReturn:      "approved",
            NotificationUrl: _mp.NotificationUrl
        );

        var content  = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await mpClient.PostAsync("/checkout/preferences", content, accessToken, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            return Result.Fail(new BadRequestError($"Error al crear preferencia en MP: {error}"));
        }

        var pref = JsonSerializer.Deserialize<MpPreferenceApiResponse>(
            await response.Content.ReadAsStringAsync(ct));

        if (pref is null)
            return Result.Fail(new BadRequestError("Respuesta inválida de MP al crear preferencia."));

        var checkoutUrl = env.IsProduction() ? pref.InitPoint : pref.SandboxInitPoint;
        return Result.Ok((pref.Id, checkoutUrl));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string GeneratePickupCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }

    private static OrderSummaryResponse MapConsumerOrder(Order o) => new()
    {
        Id                = o.Id,
        ExternalReference = o.ExternalReference,
        MerchantName      = o.Merchant.BusinessName,
        TotalAmount       = o.TotalAmount,
        OrderStatus       = o.OrderStatus.ToString(),
        PickupCode        = o.PickupCode,
        CreatedAt         = o.CreatedAt,
        Items             = o.OrderDetails.Select(od => new OrderDetailItemResponse
        {
            ProductName = od.Product.Name,
            Quantity    = od.Quantity,
            UnitPrice   = od.UnitPrice
        }).ToList()
    };

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
