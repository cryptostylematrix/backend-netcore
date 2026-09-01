using Common.Domain;

namespace ReferalProgram.Core.ProfileVolumeAggregate;

public interface IProfileVolumeRepository : IRepository<ProfileVolume>
{
    Task IncreasePersonalAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        uint amount,
        CancellationToken cancellationToken);

    Task IncreaseReferralAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        uint amount,
        CancellationToken cancellationToken);
}
