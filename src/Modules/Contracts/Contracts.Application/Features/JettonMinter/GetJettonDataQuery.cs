namespace Contracts.Application.Features.JettonMinter;

public sealed record GetJettonDataQuery(string Addr)
    : IQuery<JettonMinterDataResponse>;

internal sealed class GetJettonDataQueryHandler(IJetttonMinterQueries queries)
    : IQueryHandler<GetJettonDataQuery, JettonMinterDataResponse>
{
    public Task<Result<JettonMinterDataResponse>> Handle(
        GetJettonDataQuery request,
        CancellationToken ct) =>
        queries.GetJettonDataAsync(request.Addr, ct);
}
