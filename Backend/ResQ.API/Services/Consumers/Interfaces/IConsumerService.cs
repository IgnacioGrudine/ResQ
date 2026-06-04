using FluentResults;
using ResQ.API.DTOs.Consumers;
using ResQ.API.DTOs.Orders;

namespace ResQ.API.Services.Consumers;

public interface IConsumerService
{
    Task<Result<ConsumerProfileResponse>> GetMyProfileAsync(int consumerProfileId, CancellationToken ct = default);
    Task<Result<ConsumerProfileResponse>> UpdateMyProfileAsync(int consumerProfileId, UpdateConsumerProfileRequest request, CancellationToken ct = default);
    Task<Result<IEnumerable<OrderSummaryResponse>>> GetMyOrdersAsync(int consumerProfileId, CancellationToken ct = default);
}
