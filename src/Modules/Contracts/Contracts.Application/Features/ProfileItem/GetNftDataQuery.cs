using Contracts.Domain.Aggregates.ProfileItem;

namespace Contracts.Application.Features.ProfileItem;

public sealed record GetNftDataQuery(string Addr) : IQuery<ProfileDataResponse>;

internal sealed class GetInviteDataQueryHandler(ITonClient tonClient, IMapper mapper) : 
    IQueryHandler<GetNftDataQuery, ProfileDataResponse>
{
    public async Task<Result<ProfileDataResponse>> Handle(GetNftDataQuery request, CancellationToken ct)
    {
        var profileItemContract = ProfileItemContract.CreateFromAddress(new Address(request.Addr));
        var result = await profileItemContract.GetNftData(tonClient, ct);

        if (!result.IsSuccess)
        {
            return Result<ProfileDataResponse>.Error(new ErrorList(result.Errors));
        }
        
        var response = mapper.Map<ProfileDataResponse>(result.Value);
        return Result.Success(response);
    }
}