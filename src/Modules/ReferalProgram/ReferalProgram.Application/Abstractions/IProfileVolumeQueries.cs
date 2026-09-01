namespace ReferalProgram.Application.Abstractions;

public interface IProfileVolumeQueries
{
    Task<ProfileVolumeResponse> GetAsync(
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, uint>> GetReferralVolumesAsync(
        string marketingAddr,
        byte structureNumber,
        IReadOnlyCollection<string> profileAddresses,
        CancellationToken cancellationToken);
}
