namespace ReferalProgram.Application.Abstractions;

public interface IProfileVolumeMaintenance
{
    Task RecalculateReferralAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken);

    Task ResetReferralAsync(
        string marketingAddr,
        byte structureNumber,
        CancellationToken cancellationToken);
}
