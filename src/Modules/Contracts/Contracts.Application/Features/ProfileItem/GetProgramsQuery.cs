using Contracts.Domain.Aggregates.ProfileItem;

namespace Contracts.Application.Features.ProfileItem;

public sealed record GetProgramsQuery(string Addr) : IQuery<ProfileProgramsResponse>;

internal sealed class GetProgramsQueryHandler(ITonClient tonClient, IMapper mapper) : 
    IQueryHandler<GetProgramsQuery, ProfileProgramsResponse>
{
    public async Task<Result<ProfileProgramsResponse>> Handle(GetProgramsQuery request, CancellationToken ct)
    {
        var profileItemContract = ProfileItemContract.CreateFromAddress(new Address(request.Addr));
        var result = await profileItemContract.GetPrograms(tonClient, ct);

        if (!result.IsSuccess)
        {
            return Result<ProfileProgramsResponse>.Error(new ErrorList(result.Errors));
        }
        
        var response = mapper.Map<ProfileProgramsResponse>(result.Value);
        return Result.Success(response);
    }
}