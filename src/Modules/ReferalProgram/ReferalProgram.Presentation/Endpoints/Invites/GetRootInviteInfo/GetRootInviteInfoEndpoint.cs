using ReferalProgram.Application.Features.Invites;

namespace ReferalProgram.Presentation.Endpoints.Invites.GetRootInviteInfo;

public sealed class GetRootInviteInfoEndpoint(ISender sender)
    : Endpoint<GetRootInviteInfoRequest, InviteDataResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/root-invite-info");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get root invite information";
            summary.Description = "Gets the structure 0 root invite information for a marketing program.";
            summary.ExampleRequest = new GetRootInviteInfoRequest
            {
                MarketingAddr = "E..."
            };
            summary.ResponseExamples[StatusCodes.Status200OK] = new InviteDataResponse
            {
                ProfileAddr = "E...",
                ProfileLogin = "root",
                InviterProfileAddr = null,
                InviterProfileLogin = null,
                CreatedAt = 123456,
                ActivatedAt = 123456,
                Filling = 0,
                IsActive = true
            };
        });
    }

    public override async Task HandleAsync(GetRootInviteInfoRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetRootInviteInfoQuery(request.MarketingAddr),
            ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
