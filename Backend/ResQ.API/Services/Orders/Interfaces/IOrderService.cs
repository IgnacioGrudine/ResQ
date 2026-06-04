using FluentResults;
using ResQ.API.DTOs.Orders;

namespace ResQ.API.Services.Orders;

public interface IOrderService
{
    Task<Result<OrderCreatedResponse>> CreateOrderAsync(int consumerProfileId, CreateOrderRequest request, CancellationToken ct = default);
    Task<Result<IEnumerable<OrderSummaryResponse>>> GetConsumerOrdersAsync(int consumerProfileId, CancellationToken ct = default);
    Task<Result<OrderSummaryResponse>> GetOrderByIdAsync(int orderId, int consumerProfileId, CancellationToken ct = default);
    Task<Result<IEnumerable<MerchantOrderSummaryResponse>>> GetMerchantOrdersAsync(int merchantProfileId, CancellationToken ct = default);
    Task<Result<MerchantOrderSummaryResponse>> ConfirmPickupAsync(int merchantProfileId, string pickupCode, CancellationToken ct = default);
}
