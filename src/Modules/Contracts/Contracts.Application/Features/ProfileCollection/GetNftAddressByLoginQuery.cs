using Contracts.Domain.Aggregates.ProfileCollection;

namespace Contracts.Application.Features.ProfileCollection;

public sealed record GetNftAddressByLoginQuery(string Login) : IQuery<NftAddressResponse>;

internal sealed class GetNftAddressByLoginQueryHandler(ITonClient tonClient, ProfileCollectionContract profileCollectionContract) : 
    IQueryHandler<GetNftAddressByLoginQuery, NftAddressResponse>
{
    public async Task<Result<NftAddressResponse>> Handle(GetNftAddressByLoginQuery request, CancellationToken ct)
    {
        var index = TonHashUtils.Sha256n(request.Login);
        
        var result = await profileCollectionContract.GetNftAddressByIndex(tonClient, index, ct);

        if (!result.IsSuccess)
        {
            return Result<NftAddressResponse>.Error(new ErrorList(result.Errors));
        }

        var response = new NftAddressResponse
        {
            Addr = result.Value.ToString()
        };
        
        return Result.Success(response);
    }
}