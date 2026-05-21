using FluentResults;
using ResQ.API.Common.Errors;
using ResQ.API.Data.UnitOfWork;
using ResQ.API.DTOs.Consumers;

namespace ResQ.API.Services.Consumers;

public class ConsumerService(IUnitOfWork uow) : IConsumerService
{
    public async Task<Result<ConsumerProfileResponse>> GetMyProfileAsync(int consumerProfileId, CancellationToken ct = default)
    {
        var profile = await uow.ConsumerProfiles.GetByIdWithUserAsync(consumerProfileId, ct);
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
        var profile = await uow.ConsumerProfiles.GetByIdWithUserAsync(consumerProfileId, ct);
        if (profile is null)
            return Result.Fail(new NotFoundError("Perfil de consumidor no encontrado."));

        profile.FirstName   = request.FirstName;
        profile.LastName    = request.LastName;
        profile.PhoneNumber = request.PhoneNumber;
        profile.UpdatedAt   = DateTime.UtcNow;
        uow.ConsumerProfiles.Update(profile);

        await uow.SaveChangesAsync(ct);

        return Result.Ok(new ConsumerProfileResponse
        {
            Id          = profile.Id,
            FirstName   = profile.FirstName,
            LastName    = profile.LastName,
            Email       = profile.User.Email,
            PhoneNumber = profile.PhoneNumber
        });
    }
}
