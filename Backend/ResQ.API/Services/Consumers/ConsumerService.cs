using FluentResults;
using ResQ.API.Common.Errors;
using ResQ.API.DTOs.Consumers;
using ResQ.API.DTOs.Orders;
using ResQ.API.Repositories.Auth;
using ResQ.API.Services.Orders;

namespace ResQ.API.Services.Consumers;

public class ConsumerService(
    IConsumerProfileRepository consumerProfiles,
    IOrderService orderService) : IConsumerService
{
    public async Task<Result<ConsumerProfileResponse>> GetMyProfileAsync(int consumerProfileId, CancellationToken ct = default)
    {
        var profile = await consumerProfiles.GetByIdWithUserAsync(consumerProfileId, ct);
        if (profile is null)
            return Result.Fail(new NotFoundError("Perfil de consumidor no encontrado."));

        return Result.Ok(new ConsumerProfileResponse
        {
            Id          = profile.Id,
            FirstName   = profile.FirstName,
            LastName    = profile.LastName,
            Email       = profile.User.Email,
            PhoneNumber = profile.PhoneNumber
        });
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

        return Result.Ok(new ConsumerProfileResponse
        {
            Id          = profile.Id,
            FirstName   = profile.FirstName,
            LastName    = profile.LastName,
            Email       = profile.User.Email,
            PhoneNumber = profile.PhoneNumber
        });
    }

    public Task<Result<IEnumerable<OrderSummaryResponse>>> GetMyOrdersAsync(int consumerProfileId, CancellationToken ct = default)
        => orderService.GetConsumerOrdersAsync(consumerProfileId, ct);
}
