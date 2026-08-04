using ReferalProgram.Application.Features.Invites;

namespace ReferalProgram.Presentation.Endpoints.Invites.GetInviteInfo;

public sealed class GetInviteInfoEndpoint(ISender sender)
    : Endpoint<GetInviteInfoRequest, InviteDataResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/invite-info");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get invite information";
            summary.Description = "Gets structure 0 invite information for a profile.";
            summary.ExampleRequest = new GetInviteInfoRequest
            {
                MarketingAddr = "E...",
                ProfileAddr = "E..."
            };
            summary.ResponseExamples[StatusCodes.Status200OK] = new InviteDataResponse
            {
                ProfileAddr = "E...",
                ProfileLogin = "login",
                InviterProfileAddr = "E...",
                InviterProfileLogin = "inviter",
                CreatedAt = 123456,
                ActivatedAt = 123456,
                Filling = 0,
                IsActive = true
            };
        });
    }

    public override async Task HandleAsync(GetInviteInfoRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetInviteInfoQuery(request.MarketingAddr, request.ProfileAddr),
            ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
