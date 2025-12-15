using Contracts.Domain.Aggregates.Invite;

namespace Contracts.Application.Features.Invite;

public sealed record GetInviteAddrBySeqNoQuery(string Addr, uint SeqNo) : IQuery<InviteAddressResponse>;

internal sealed class GetInviteAddressBySeqNoQueryHandler(ITonClient tonClient) : 
    IQueryHandler<GetInviteAddrBySeqNoQuery, InviteAddressResponse>
{
    public async Task<Result<InviteAddressResponse>> Handle(GetInviteAddrBySeqNoQuery request, CancellationToken ct)
    {
        var inviteContract = InviteContract.CreateFromAddress(new Address(request.Addr));
        var result = await inviteContract.GetInviteAddressBySeqNo(tonClient, request.SeqNo, ct);

        if (!result.IsSuccess)
        {
            return Result<InviteAddressResponse>.Error(new ErrorList(result.Errors));
        }

        var response = new InviteAddressResponse
        {
            Addr = result.Value.ToString()
        };
        
        return Result.Success(response);
    }
}