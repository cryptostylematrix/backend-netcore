using Contracts.Application.Features.Invite;

namespace Contracts.Presentation.Endpoints.Invite.GetInviteData;

public sealed class GetInviteDataEndpoint(ISender sender) : Endpoint<GetInviteDataRequest, InviteDataResponse>
{
    public override void Configure()
    {
        Get("contracts/invite/{addr}/data");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Invite data";
            s.Description = "Get Invite data";
            s.ExampleRequest = new GetInviteDataRequest
            {
                Addr = ""
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new InviteDataResponse
            {
                AdminAddr = "",
                Program = 1,
                NextRefNo = 10,
                Number  = 11,
                ParentAddr= "",
                Owner = new InviteOwnerResponse
                {
                    OwnerAddr = "",
                    SetAt = (ulong)DateTime.Now.Ticks
                }
            };
        });
    }

    public override async Task HandleAsync(GetInviteDataRequest request, CancellationToken ct)
    {
        var query = new GetInviteDataQuery(request.Addr);
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