using FluentResults;
using ResQ.API.DTOs.Orders;

namespace ResQ.API.Services.Orders;

public interface IOrderService
{
    Task<Result<IEnumerable<OrderSummaryResponse>>> GetMyOrdersAsync(int consumerProfileId, CancellationToken ct = default);
}
