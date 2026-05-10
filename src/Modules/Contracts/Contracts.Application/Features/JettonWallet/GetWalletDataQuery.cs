namespace Contracts.Application.Features.JettonWallet;


public sealed record GetWalletDataQuery(string Addr) : IQuery<JettonWalletDataResponse>;

internal sealed class GetWalletAddressQueryHandler(IJettonWalletQueries queries)
    : IQueryHandler<GetWalletDataQuery, JettonWalletDataResponse>
{
    public Task<Result<JettonWalletDataResponse>> Handle(GetWalletDataQuery request, CancellationToken ct)
        => queries.GetWalletDataAsync(
            addr: request.Addr,
            ct);
}