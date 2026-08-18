using UI.Application.Abstractions;
using UI.Application.Services;
using UI.Core.ProfileAggregate;
using UI.Core.WalletProfileIntentAggregate;

namespace UI.Application.Features.ProfileIntents;

public sealed record CheckWalletProfilesCommand(string WalletAddr)
    : ICommand<CheckWalletProfilesResponse>;

internal sealed class CheckWalletProfilesCommandHandler(
    IWalletAddressService walletAddressService,
    ProfileSynchronizer profileSynchronizer,
    ICachedProfileRepository profileRepository,
    IWalletProfileIntentRepository intentRepository,
    IWalletProfileQueries queries,
    IUiUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<CheckWalletProfilesCommand, CheckWalletProfilesResponse>
{
    public async Task<Result<CheckWalletProfilesResponse>> Handle(
        CheckWalletProfilesCommand request,
        CancellationToken cancellationToken)
    {
        if (!walletAddressService.TryNormalize(request.WalletAddr, out var walletAddr))
        {
            return Result.Success(new CheckWalletProfilesResponse
            {
                Success = false,
                Errors = [UiErrorCodes.InvalidWalletAddress]
            });
        }

        var intents = await intentRepository.ListAsync(walletAddr, cancellationToken);
        var errors = new HashSet<string>(StringComparer.Ordinal);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var intent in intents)
        {
            intent.NormalizeWalletAddress(walletAddr, now);
            var cachedProfile = await profileRepository.GetByAddressAsync(
                intent.ProfileAddr,
                cancellationToken);
            if (cachedProfile is null)
            {
                errors.Add(UiErrorCodes.ProfileNotFound);
                continue;
            }

            var synchronization = await profileSynchronizer.SynchronizeAsync(
                cachedProfile.Login,
                cancellationToken);
            if (!synchronization.IsSuccess)
            {
                errors.Add(synchronization.ErrorCode
                    ?? UiErrorCodes.ContractRequestFailed);
                continue;
            }

            intent.UpdateOwnership(
                walletAddressService.AreEqual(
                    walletAddr,
                    synchronization.Snapshot!.OwnerAddr),
                now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var profiles = await queries.ListAsync(walletAddr, cancellationToken);

        return Result.Success(new CheckWalletProfilesResponse
        {
            Success = errors.Count == 0,
            Errors = errors.ToArray(),
            Profiles = profiles
        });
    }
}
