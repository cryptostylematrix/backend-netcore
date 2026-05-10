namespace Contracts.Application.Features.JettonMinter;


public sealed record GetWalletAddressQuery(string Addr, string OwnerAddr) : IQuery<JettonWalletAddressResponse>;

internal sealed class GetWalletAddressQueryHandler(IJetttonMinterQueries queries)
    : IQueryHandler<GetWalletAddressQuery, JettonWalletAddressResponse>
{
    public Task<Result<JettonWalletAddressResponse>> Handle(GetWalletAddressQuery request, CancellationToken ct)
        => queries.GetWalletAddressAsync(
            addr: request.Addr,
            ownerAddr: request.OwnerAddr, 
            ct);
}