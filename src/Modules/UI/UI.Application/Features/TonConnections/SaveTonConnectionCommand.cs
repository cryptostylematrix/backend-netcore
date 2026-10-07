using UI.Application.Abstractions;
using UI.Core.TonConnectionAggregate;

namespace UI.Application.Features.TonConnections;

public sealed record SaveTonConnectionCommand(string WalletAddr, string? WalletStateInit,
    string? WalletName, string? AppVersion, string? Platform) : ICommand<TonConnectionResponse>;

internal sealed class SaveTonConnectionCommandHandler(
    IWalletAddressService walletAddressService,
    IWalletContractVersionResolver versionResolver,
    ITonConnectionRepository repository,
    TimeProvider timeProvider) : ICommandHandler<SaveTonConnectionCommand, TonConnectionResponse>
{
    public async Task<Result<TonConnectionResponse>> Handle(
        SaveTonConnectionCommand request, CancellationToken cancellationToken)
    {
        if (!walletAddressService.TryNormalize(request.WalletAddr, out var walletAddr))
            return Failure(UiErrorCodes.InvalidWalletAddress);

        if (!ValidText(request.WalletName, 128) || !ValidText(request.AppVersion, 64)
            || !ValidText(request.Platform, 32) || request.WalletStateInit?.Length > 65536)
            return Failure(UiErrorCodes.InvalidTonConnection);

        if (!versionResolver.TryResolve(walletAddr, request.WalletStateInit, out var version))
            return Failure(UiErrorCodes.InvalidWalletStateInit);

        var connection = TonConnection.Create(walletAddr, version,
            request.WalletName!.Trim(), request.AppVersion!.Trim(), request.Platform!.Trim(),
            timeProvider.GetUtcNow().UtcDateTime);
        await repository.UpsertAsync(connection, cancellationToken);
        return Result.Success(new TonConnectionResponse { Success = true });
    }

    private static bool ValidText(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maxLength
        && !value.Any(char.IsControl);

    private static Result<TonConnectionResponse> Failure(string error) =>
        Result.Success(new TonConnectionResponse { Errors = [error] });
}
