using FluentResults;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Reviews;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Reviews;
using ResQ.API.Repositories.Orders;
using ResQ.API.Repositories.Reviews;

namespace ResQ.API.Services.Reviews;

/// <summary>
/// Handles the consumer-facing review submission flow, enforcing all domain invariants
/// (order ownership, pickup status, and one-review-per-order uniqueness) before persisting.
/// </summary>
public class ReviewService(
    IOrderRepository orders,
    IReviewRepository reviews) : IReviewService
{
    /// <summary>
    /// Validates the order state and creates a new review record for the given consumer.
    /// Fails fast if the order is missing, belongs to someone else, has not been picked up yet,
    /// or already has a review.
    /// </summary>
    public async Task<Result<ReviewResponse>> CreateReviewAsync(
        int consumerProfileId, int orderId, CreateReviewRequest request, CancellationToken ct = default)
    {
        var order = await orders.GetByIdAsync(orderId, ct);

        if (order is null)
            return Result.Fail(new NotFoundError("Orden no encontrada."));

        if (order.ConsumerId != consumerProfileId)
            return Result.Fail(new ForbiddenError("No tenés permiso para reseñar esta orden."));

        if (order.OrderStatus != OrderStatus.PickedUp)
            return Result.Fail(new BadRequestError("Solo podés reseñar órdenes que ya retiraste."));

        if (await reviews.ExistsForOrderAsync(orderId, ct))
            return Result.Fail(new ConflictError("Ya dejaste una reseña para esta orden."));

        var review = new Review
        {
            OrderId    = orderId,
            MerchantId = order.MerchantId,
            Rating     = request.Rating,
            Comment    = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedAt  = DateTime.UtcNow
        };

        await reviews.AddAsync(review, ct);
        await reviews.SaveChangesAsync(ct);

        return Result.Ok(new ReviewResponse
        {
            Id        = review.Id,
            Rating    = review.Rating,
            Comment   = review.Comment,
            CreatedAt = review.CreatedAt
        });
    }
}
