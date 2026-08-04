using ReferalProgram.Application.Features.Invites;

namespace ReferalProgram.Presentation.Endpoints.Invites.GetReferrals;

public sealed class GetReferralsEndpoint(ISender sender)
    : Endpoint<GetReferralsRequest, Paginated<InviteDataResponse>>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/referrals");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get referrals";
            summary.Description = "Gets direct structure 0 referrals for a profile.";
            summary.ExampleRequest = new GetReferralsRequest
            {
                MarketingAddr = "E...",
                ProfileAddr = "E...",
                Page = 1,
                PageSize = 20
            };
            summary.ResponseExamples[StatusCodes.Status200OK] = new Paginated<InviteDataResponse>
            {
                Items =
                [
                    new InviteDataResponse
                    {
                        ProfileAddr = "E...",
                        ProfileLogin = "login",
                        InviterProfileAddr = "E...",
                        InviterProfileLogin = "inviter",
                        CreatedAt = 123456,
                        ActivatedAt = 123456,
                        Filling = 0,
                        IsActive = true
                    }
                ],
                Page = 1,
                TotalPages = 1
            };
        });
    }

    public override async Task HandleAsync(GetReferralsRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetReferralsQuery(
                request.MarketingAddr,
                request.ProfileAddr,
                request.Page,
                request.PageSize),
            ct);

        Response = result.Value;
    }
}
