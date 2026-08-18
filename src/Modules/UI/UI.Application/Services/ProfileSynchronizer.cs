using UI.Application.Abstractions;
using UI.Core.ProfileAggregate;

namespace UI.Application.Services;

internal sealed class ProfileSynchronizer(
    IProfileContractReader contractReader,
    ICachedProfileRepository profileRepository,
    TimeProvider timeProvider)
{
    public async Task<ProfileSynchronizationResult> SynchronizeAsync(
        string login,
        CancellationToken cancellationToken)
    {
        var lookup = await contractReader.GetByLoginAsync(login, cancellationToken);
        if (!lookup.IsSuccess)
        {
            return ProfileSynchronizationResult.Failure(
                lookup.ErrorCode ?? UiErrorCodes.ContractRequestFailed);
        }

        var snapshot = lookup.Profile!;
        var cachedProfile = await profileRepository.GetByAddressAsync(
            snapshot.Address,
            cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (cachedProfile is null)
        {
            cachedProfile = CachedProfile.Create(
                snapshot.Address,
                snapshot.Login,
                snapshot.ContentJson,
                now);
            profileRepository.Add(cachedProfile);
        }
        else
        {
            cachedProfile.Refresh(snapshot.Login, snapshot.ContentJson, now);
        }

        return ProfileSynchronizationResult.Success(snapshot, cachedProfile);
    }
}

internal sealed record ProfileSynchronizationResult(
    ProfileContractSnapshot? Snapshot,
    CachedProfile? CachedProfile,
    string? ErrorCode)
{
    public bool IsSuccess => Snapshot is not null && CachedProfile is not null;

    public static ProfileSynchronizationResult Success(
        ProfileContractSnapshot snapshot,
        CachedProfile cachedProfile) =>
        new(snapshot, cachedProfile, null);

    public static ProfileSynchronizationResult Failure(string errorCode) =>
        new(null, null, errorCode);
}
