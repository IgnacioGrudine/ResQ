using FluentResults;
using ResQ.API.DTOs.Consumers;
using ResQ.API.DTOs.Orders;

namespace ResQ.API.Services.Consumers;

public interface IConsumerService
{
    /// <summary>
    /// Returns the profile information of the authenticated consumer.
    /// </summary>
    /// <param name="consumerProfileId">The identifier of the consumer profile to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a <see cref="ConsumerProfileResponse"/>
    /// with the consumer's personal data; or a failed result if the profile does not exist.
    /// </returns>
    Task<Result<ConsumerProfileResponse>> GetMyProfileAsync(int consumerProfileId, CancellationToken ct = default);

    /// <summary>
    /// Updates the mutable fields of the authenticated consumer's profile.
    /// </summary>
    /// <param name="consumerProfileId">The identifier of the consumer profile to update.</param>
    /// <param name="request">DTO containing the fields to update, such as first name, last name, and phone number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing the updated <see cref="ConsumerProfileResponse"/>;
    /// or a failed result if the profile does not exist or the request data is invalid.
    /// </returns>
    Task<Result<ConsumerProfileResponse>> UpdateMyProfileAsync(int consumerProfileId, UpdateConsumerProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Returns the order history of the authenticated consumer, ordered from most recent to oldest.
    /// </summary>
    /// <param name="consumerProfileId">The identifier of the consumer profile whose orders are requested.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing a collection of <see cref="OrderSummaryResponse"/>
    /// items; or a failed result if the profile does not exist.
    /// </returns>
    Task<Result<IEnumerable<OrderSummaryResponse>>> GetMyOrdersAsync(int consumerProfileId, CancellationToken ct = default);
}
