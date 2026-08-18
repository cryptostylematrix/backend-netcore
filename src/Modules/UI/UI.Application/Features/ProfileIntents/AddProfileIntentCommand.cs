using UI.Application.Abstractions;
using UI.Application.Services;
using UI.Core.WalletProfileIntentAggregate;

namespace UI.Application.Features.ProfileIntents;

public sealed record AddProfileIntentCommand(
    string WalletAddr,
    string Login,
    ProfileModeResponse? Mode) : ICommand<ProfileIntentOperationResponse>;

internal sealed class AddProfileIntentCommandHandler(
    IWalletAddressService walletAddressService,
    ProfileSynchronizer profileSynchronizer,
    IWalletProfileIntentRepository intentRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AddProfileIntentCommand, ProfileIntentOperationResponse>
{
    public async Task<Result<ProfileIntentOperationResponse>> Handle(
        AddProfileIntentCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WalletAddr))
            return Result.Success(Failure(UiErrorCodes.WalletRequired));

        if (!walletAddressService.TryNormalize(
                request.WalletAddr,
                out var walletAddr))
        {
            return Result.Success(Failure(UiErrorCodes.InvalidWalletAddress));
        }

        if (string.IsNullOrWhiteSpace(request.Login))
            return Result.Success(Failure(UiErrorCodes.InvalidLogin));

        if (request.Mode is null)
            return Result.Success(Failure(UiErrorCodes.InvalidProfileMode));

        var synchronization = await profileSynchronizer.SynchronizeAsync(
            request.Login.Trim().ToLowerInvariant(),
            cancellationToken);
        if (!synchronization.IsSuccess)
        {
            return Result.Success(Failure(
                synchronization.ErrorCode ?? UiErrorCodes.ContractRequestFailed));
        }

        var snapshot = synchronization.Snapshot!;
        var owned = walletAddressService.AreEqual(walletAddr, snapshot.OwnerAddr);
        var requestedMode = ToDomainMode(request.Mode.Value);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var existing = await intentRepository.GetAsync(
            walletAddr,
            snapshot.Address,
            cancellationToken);

        if (existing is not null)
        {
            existing.NormalizeWalletAddress(walletAddr, now);
            existing.UpdateOwnership(owned, now);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (requestedMode == WalletProfileMode.Owner && !owned)
            {
                return Result.Success(Failure(
                    UiErrorCodes.ProfileDoesNotBelongToWallet,
                    [ProfileModeResponse.Preview]));
            }

            return Result.Success(Success(owned));
        }

        if (requestedMode == WalletProfileMode.Owner && !owned)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(Failure(
                UiErrorCodes.ProfileDoesNotBelongToWallet,
                [ProfileModeResponse.Preview]));
        }

        intentRepository.Add(WalletProfileIntent.Add(
            walletAddr,
            snapshot.Address,
            requestedMode,
            owned,
            now));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Success(owned));
    }

    private static WalletProfileMode ToDomainMode(ProfileModeResponse mode) =>
        mode == ProfileModeResponse.Owner
            ? WalletProfileMode.Owner
            : WalletProfileMode.Preview;

    private static ProfileIntentOperationResponse Success(bool owned) => new()
    {
        Success = true,
        AvailableModes = owned
            ? [ProfileModeResponse.Owner, ProfileModeResponse.Preview]
            : [ProfileModeResponse.Preview]
    };

    private static ProfileIntentOperationResponse Failure(
        string error,
        IReadOnlyCollection<ProfileModeResponse>? availableModes = null) => new()
    {
        Success = false,
        Errors = [error],
        AvailableModes = availableModes ?? []
    };
}
