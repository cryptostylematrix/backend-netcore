namespace Contracts.Application.Features.JettonWallet;

public sealed record GetMetadataQuery(string Addr) : IQuery<JettonMetadataResponse>;

internal sealed class GetMetadataQueryHandler(IJettonMetadataQueries queries)
    : IQueryHandler<GetMetadataQuery, JettonMetadataResponse>
{
    public Task<Result<JettonMetadataResponse>> Handle(GetMetadataQuery request, CancellationToken ct) =>
        queries.GetByWalletAsync(request.Addr, ct);
}
