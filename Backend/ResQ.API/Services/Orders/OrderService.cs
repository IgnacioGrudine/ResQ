using FluentResults;
using ResQ.API.Data.UnitOfWork;
using ResQ.API.DTOs.Orders;
using ResQ.API.DTOs.Shared;

namespace ResQ.API.Services.Orders;

public class OrderService(IUnitOfWork uow) : IOrderService
{
    public async Task<Result<IEnumerable<OrderSummaryResponse>>> GetMyOrdersAsync(
        int consumerProfileId, CancellationToken ct = default)
    {
        var orders = await uow.Orders.GetByConsumerIdAsync(consumerProfileId, ct);

        var result = orders.Select(o => new OrderSummaryResponse
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

        return Result.Ok(result);
    }
}
