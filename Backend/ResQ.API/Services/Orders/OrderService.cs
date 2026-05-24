using FluentResults;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Shared;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Orders;
using ResQ.API.Repositories.Orders;

namespace ResQ.API.Services.Orders;

public class OrderService(IOrderRepository orders) : IOrderService
{
    public async Task<Result<IEnumerable<OrderSummaryResponse>>> GetConsumerOrdersAsync(
        int consumerProfileId, CancellationToken ct = default)
    {
        var result = await orders.GetByConsumerIdAsync(consumerProfileId, ct);

        var response = result.Select(o => new OrderSummaryResponse
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
        });

        return Result.Ok(response);
    }

    public async Task<Result<IEnumerable<MerchantOrderSummaryResponse>>> GetMerchantOrdersAsync(
        int merchantProfileId, CancellationToken ct = default)
    {
        var result = await orders.GetByMerchantIdAsync(merchantProfileId, ct);
        return Result.Ok(result.Select(MapMerchantOrder));
    }

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
