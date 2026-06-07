using FluentResults;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Consumers;
using ResQ.API.DTOs.Orders;
using ResQ.API.Models.Auth;
using ResQ.API.Models.Enums;
using ResQ.API.Models.Orders;
using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Orders;
using ResQ.API.Services.Orders;

namespace ResQ.API.Services.Consumers;

/// <summary>
/// Manages consumer profile operations and provides consumers with access to their order history.
/// </summary>
public class ConsumerService(
    IConsumerProfileRepository consumerProfiles,
    IOrderService orderService,
    IOrderRepository orderRepository) : IConsumerService
{
    /// <summary>
    /// Retrieves the authenticated consumer's profile, including aggregated statistics
    /// such as total completed orders, total money saved, and estimated CO₂ saved.
    /// </summary>
    /// <param name="consumerProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{ConsumerProfileResponse}"/> with the profile data,
    /// or a <c>NotFoundError</c> if no profile exists for the given ID.
    /// </returns>
    public async Task<Result<ConsumerProfileResponse>> GetMyProfileAsync(int consumerProfileId, CancellationToken ct = default)
    {
        var profile = await consumerProfiles.GetByIdWithUserAsync(consumerProfileId, ct);
        if (profile is null)
            return Result.Fail(new NotFoundError("Perfil de consumidor no encontrado."));

        var orders = await orderRepository.GetByConsumerIdAsync(consumerProfileId, ct);
        return Result.Ok(BuildResponse(profile, orders));
    }

    /// <summary>
    /// Updates the authenticated consumer's editable profile fields (name and phone number)
    /// and returns the refreshed profile with recalculated statistics.
    /// </summary>
    /// <param name="consumerProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="request">The update payload containing the new first name, last name, and phone number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{ConsumerProfileResponse}"/> with the updated profile data,
    /// or a <c>NotFoundError</c> if no profile exists for the given ID.
    /// </returns>
    public async Task<Result<ConsumerProfileResponse>> UpdateMyProfileAsync(
        int consumerProfileId, UpdateConsumerProfileRequest request, CancellationToken ct = default)
    {
        var profile = await consumerProfiles.GetByIdWithUserAsync(consumerProfileId, ct);
        if (profile is null)
            return Result.Fail(new NotFoundError("Perfil de consumidor no encontrado."));

        profile.FirstName   = request.FirstName;
        profile.LastName    = request.LastName;
        profile.PhoneNumber = request.PhoneNumber;
        profile.UpdatedAt   = DateTime.UtcNow;
        consumerProfiles.Update(profile);
        await consumerProfiles.SaveChangesAsync(ct);

        var orders = await orderRepository.GetByConsumerIdAsync(consumerProfileId, ct);
        return Result.Ok(BuildResponse(profile, orders));
    }

    /// <summary>
    /// Returns a summary list of all orders placed by the consumer.
    /// </summary>
    /// <param name="consumerProfileId">The profile ID extracted from the authenticated user's JWT claims.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> containing an enumerable of <see cref="OrderSummaryResponse"/>.
    /// Delegates to <see cref="IOrderService.GetConsumerOrdersAsync"/>.
    /// </returns>
    public Task<Result<IEnumerable<OrderSummaryResponse>>> GetMyOrdersAsync(int consumerProfileId, CancellationToken ct = default)
        => orderService.GetConsumerOrdersAsync(consumerProfileId, ct);

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="ConsumerProfileResponse"/> DTO from a <see cref="ConsumerProfile"/> entity
    /// and the consumer's order history, computing aggregated sustainability and savings metrics.
    /// </summary>
    /// <param name="profile">The consumer profile entity with the associated <see cref="User"/> loaded.</param>
    /// <param name="orders">All orders belonging to this consumer, with their order details and products loaded.</param>
    /// <returns>A fully populated <see cref="ConsumerProfileResponse"/> DTO.</returns>
    /// <remarks>
    /// Cancelled orders are excluded from all aggregated metrics.
    /// CO₂ saved is estimated at a fixed rate of 0.5 kg per completed order, based on
    /// the food waste reduction impact assumption used in the ResQ platform.
    /// Total saved is clamped to zero if the calculation produces a negative value (e.g., promotional packs).
    /// </remarks>
    private static ConsumerProfileResponse BuildResponse(ConsumerProfile profile, IEnumerable<Order> orders)
    {
        var completed  = orders.Where(o => o.OrderStatus != OrderStatus.Cancelled).ToList();
        var totalSaved = completed
            .SelectMany(o => o.OrderDetails)
            .Sum(od => (od.Product.OriginalPrice - od.UnitPrice) * od.Quantity);

        return new ConsumerProfileResponse
        {
            Id          = profile.Id,
            FirstName   = profile.FirstName,
            LastName    = profile.LastName,
            Email       = profile.User.Email,
            PhoneNumber = profile.PhoneNumber,
            TotalOrders = completed.Count,
            TotalSaved  = totalSaved > 0 ? totalSaved : 0,
            Co2SavedKg  = Math.Round(completed.Count * 0.5m, 1)
        };
    }
}
