using Contracts.Application.Features.ProfileItem;

namespace Contracts.Presentation.Endpoints.ProfileItem.BuildMultiChooseInviterBody;


public sealed class BuildMultiChooseInviterBodyEndpoint(ISender sender) : 
    Endpoint<BuildMultiChooseInviterBodyRequest, MultiChooseInviterBodyResponse>
{
    public override void Configure()
    {
        Get("contracts/profile-item/body/choose-inviter/multi");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Build Choose Inviter Body";
            s.Description = "Build Choose Inviter Body";
            s.ExampleRequest = new BuildMultiChooseInviterBodyRequest
            {
                ProfileAddr = "E...",
                InviterAddr =  "E...",
                SeqNo = 1,
                InviteAddr =  "E...",
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new MultiChooseInviterBodyResponse
            {
                Boc = "..."
            };
        });
    }

    public override async Task HandleAsync(BuildMultiChooseInviterBodyRequest request, CancellationToken ct)
    {
        var query = new BuildMultiChooseInviterBodyQuery(
            ProfileAddr: request.ProfileAddr,
            InviterAddr: request.InviterAddr,
            SeqNo: request.SeqNo,
            InviteAddr: request.InviteAddr);
            
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