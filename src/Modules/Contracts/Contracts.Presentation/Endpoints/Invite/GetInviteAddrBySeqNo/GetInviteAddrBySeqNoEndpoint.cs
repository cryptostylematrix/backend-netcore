using Contracts.Application.Features.Invite;

namespace Contracts.Presentation.Endpoints.Invite.GetInviteAddrBySeqNo;

public sealed class GetInviteAddrBySeqNoEndpoint(ISender sender) : 
    Endpoint<GetInviteAddrBySeqNoRequest, InviteAddressResponse>
{
    public override void Configure()
    {
        Get("contracts/invite/{addr}/invite-addr-by-seq-no/{seqNo}");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Invite Addr by Seq No";
            s.Description = "Get Invite Addr by Seq No";
            s.ExampleRequest = new GetInviteAddrBySeqNoRequest
            {
                Addr = "E...",
                SeqNo = 20
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new InviteAddressResponse
            {
               Addr = "E..."
            };
        });
    }

    public override async Task HandleAsync(GetInviteAddrBySeqNoRequest request, CancellationToken ct)
    {
        var query = new GetInviteAddrBySeqNoQuery(request.Addr, request.SeqNo);
        var result = await sender.Send(query, ct);
        
        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
        }
        else
        {
            Response = result.Value;
        }
    }
}