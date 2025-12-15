using Contracts.Domain.Aggregates.Invite;

namespace Contracts.Application.Features.Invite;

public sealed record GetInviteDataQuery(string Addr) : IQuery<InviteDataResponse>;
 
 internal sealed class GetInviteDataQueryHandler(ITonClient tonClient, IMapper mapper) : 
     IQueryHandler<GetInviteDataQuery, InviteDataResponse>
 {
     public async Task<Result<InviteDataResponse>> Handle(GetInviteDataQuery request, CancellationToken ct)
     {
         var inviteContract = InviteContract.CreateFromAddress(new Address(request.Addr));
         var result = await inviteContract.GetInviteData(tonClient, ct);
 
         if (!result.IsSuccess)
         {
             return Result<InviteDataResponse>.Error(new ErrorList(result.Errors));
         }
         
         var response = mapper.Map<InviteDataResponse>(result.Value);
         return Result.Success(response);
     }
 }