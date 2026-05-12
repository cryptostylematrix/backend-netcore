namespace Contracts.Application.Features.Marketing;

public sealed record BuildPayBonusBodyQuery(
    uint Key,
    string WalletAddr) : IQuery<PayBonusBodyResponse>;


internal sealed class BuildPayBonusBodyQueryHandler(IMarketingQueries queries)
    : IQueryHandler<BuildPayBonusBodyQuery, PayBonusBodyResponse>
{
    public Task<Result<PayBonusBodyResponse>> Handle(BuildPayBonusBodyQuery request, CancellationToken ct)
        => Task.FromResult(queries.BuildPayBonusBody(
            queryId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            key: request.Key,
            walletAddr: request.WalletAddr));
}