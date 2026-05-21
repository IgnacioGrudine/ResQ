using FluentResults;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Shared;
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

        var response = result.Select(o => new MerchantOrderSummaryResponse
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
        });

        return Result.Ok(response);
    }
}
