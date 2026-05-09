using Contracts.Application.Features.ProfileItem;

namespace Contracts.Presentation.Endpoints.ProfileItem.BuildChooseInviterBody;

public sealed class BuildChooseInviterBodyEndpoint(ISender sender) : 
    Endpoint<BuildChooseInviterBodyRequest, ChooseInviterBodyResponse>
{
    public override void Configure()
    {
        Get("contracts/profile-item/body/choose-inviter");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Build Choose Inviter Body";
            s.Description = "Build Choose Inviter Body";
            s.ExampleRequest = new BuildChooseInviterBodyRequest
            {
                Program = 123,
                InviterAddr =  "E...",
                SeqNo = 1,
                InviteAddr =  "E...",
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new ChooseInviterBodyResponse
            {
                BocHex = "..."
            };
        });
    }

    public override async Task HandleAsync(BuildChooseInviterBodyRequest request, CancellationToken ct)
    {
        var query = new BuildChooseInviterBodyQuery(
            Program: request.Program,
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