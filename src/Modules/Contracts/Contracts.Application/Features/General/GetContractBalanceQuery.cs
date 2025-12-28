namespace Contracts.Application.Features.General;

public sealed record GetContractBalanceQuery(string Addr) : IQuery<ContractBalanceResponse>;

internal sealed class GetContractBalanceQueryHandler(IGeneralQueries generalQueries)
    : IQueryHandler<GetContractBalanceQuery, ContractBalanceResponse>
{
    public Task<Result<ContractBalanceResponse>> Handle(GetContractBalanceQuery request, CancellationToken ct)
        => generalQueries.GetContractBalanceAsync(request.Addr, ct);
}