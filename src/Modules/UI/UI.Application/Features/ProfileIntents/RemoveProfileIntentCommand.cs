using UI.Application.Abstractions;
using UI.Core.WalletProfileIntentAggregate;

namespace UI.Application.Features.ProfileIntents;

public sealed record RemoveProfileIntentCommand(
    string WalletAddr,
    string Login) : ICommand<ProfileIntentOperationResponse>;

internal sealed class RemoveProfileIntentCommandHandler(
    IWalletAddressService walletAddressService,
    IWalletProfileIntentRepository intentRepository,
    IUiUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<RemoveProfileIntentCommand, ProfileIntentOperationResponse>
{
    public async Task<Result<ProfileIntentOperationResponse>> Handle(
        RemoveProfileIntentCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WalletAddr))
            return Result.Success(Failure(UiErrorCodes.WalletRequired));

        if (!walletAddressService.TryNormalize(request.WalletAddr, out var walletAddr))
            return Result.Success(Failure(UiErrorCodes.InvalidWalletAddress));

        if (string.IsNullOrWhiteSpace(request.Login))
            return Result.Success(Failure(UiErrorCodes.InvalidLogin));

        var intent = await intentRepository.GetByLoginAsync(
            walletAddr,
            request.Login.Trim().ToLowerInvariant(),
            cancellationToken);
        if (intent is null)
            return Result.Success(Failure(UiErrorCodes.RelationshipNotFound));

        var now = timeProvider.GetUtcNow().UtcDateTime;
        intent.NormalizeWalletAddress(walletAddr, now);
        intent.Remove(now);
        intentRepository.Remove(intent);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ProfileIntentOperationResponse { Success = true });
    }

    private static ProfileIntentOperationResponse Failure(string error) => new()
    {
        Success = false,
        Errors = [error]
    };
}
