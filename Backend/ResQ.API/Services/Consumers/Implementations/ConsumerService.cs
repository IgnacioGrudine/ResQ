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

public class ConsumerService(
    IConsumerProfileRepository consumerProfiles,
    IOrderService orderService,
    IOrderRepository orderRepository) : IConsumerService
{
    public async Task<Result<ConsumerProfileResponse>> GetMyProfileAsync(int consumerProfileId, CancellationToken ct = default)
    {
        var profile = await consumerProfiles.GetByIdWithUserAsync(consumerProfileId, ct);
        if (profile is null)
            return Result.Fail(new NotFoundError("Perfil de consumidor no encontrado."));

        var orders = await orderRepository.GetByConsumerIdAsync(consumerProfileId, ct);
        return Result.Ok(BuildResponse(profile, orders));
    }

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

    public Task<Result<IEnumerable<OrderSummaryResponse>>> GetMyOrdersAsync(int consumerProfileId, CancellationToken ct = default)
        => orderService.GetConsumerOrdersAsync(consumerProfileId, ct);

    // ── Helpers ─────────────────────────────────────────────────────────────

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
