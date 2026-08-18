using UI.Application.Abstractions;

namespace UI.Application.Features.ProfileIntents;

public sealed record GetWalletProfilesQuery(string WalletAddr)
    : IQuery<IReadOnlyCollection<WalletProfileResponse>>;

internal sealed class GetWalletProfilesQueryHandler(
    IWalletAddressService walletAddressService,
    IWalletProfileQueries queries)
    : IQueryHandler<GetWalletProfilesQuery, IReadOnlyCollection<WalletProfileResponse>>
{
    public async Task<Result<IReadOnlyCollection<WalletProfileResponse>>> Handle(
        GetWalletProfilesQuery request,
        CancellationToken cancellationToken)
    {
        if (!walletAddressService.TryNormalize(request.WalletAddr, out var walletAddr))
            return Result.Error(UiErrorCodes.InvalidWalletAddress);

        return Result.Success(await queries.ListAsync(walletAddr, cancellationToken));
    }
}
