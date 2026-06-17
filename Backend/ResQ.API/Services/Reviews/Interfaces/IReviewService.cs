using FluentResults;
using ResQ.API.DTOs.Reviews;

namespace ResQ.API.Services.Reviews;

public interface IReviewService
{
    /// <summary>
    /// Creates a review for the order identified by <paramref name="orderId"/> on behalf
    /// of the authenticated consumer.
    /// </summary>
    /// <remarks>
    /// The operation enforces the following domain invariants:
    /// <list type="bullet">
    ///   <item>The order must exist and belong to the requesting consumer.</item>
    ///   <item>The order must be in <c>PickedUp</c> status (the consumer must have already
    ///         collected their pack before they can rate the experience).</item>
    ///   <item>Only one review is allowed per order. A second submission for the same
    ///         order is rejected with a <c>ConflictError</c>.</item>
    /// </list>
    /// </remarks>
    /// <param name="consumerProfileId">The profile ID of the consumer submitting the review, extracted from their JWT claims.</param>
    /// <param name="orderId">The identifier of the order being reviewed.</param>
    /// <param name="request">DTO containing the numeric rating and optional comment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing the newly created <see cref="ReviewResponse"/>;
    /// or a failed result with a <c>NotFoundError</c> if the order does not exist,
    /// a <c>ForbiddenError</c> if the order belongs to another consumer,
    /// a <c>BadRequestError</c> if the order has not yet been picked up,
    /// or a <c>ConflictError</c> if a review for the order already exists.
    /// </returns>
    Task<Result<ReviewResponse>> CreateReviewAsync(
        int consumerProfileId, int orderId, CreateReviewRequest request, CancellationToken ct = default);
}
