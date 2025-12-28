namespace Contracts.Application.Features.ProfileCollection;

public sealed record GetNftAddressByLoginQuery(string Login) : IQuery<NftAddressResponse>;

internal sealed class GetNftAddressByLoginQueryHandler(IProfileCollectionQueries queries)
    : IQueryHandler<GetNftAddressByLoginQuery, NftAddressResponse>
{
    public Task<Result<NftAddressResponse>> Handle(GetNftAddressByLoginQuery request, CancellationToken ct)
        => queries.GetNftAddressByLoginAsync(request.Login, ct);
}