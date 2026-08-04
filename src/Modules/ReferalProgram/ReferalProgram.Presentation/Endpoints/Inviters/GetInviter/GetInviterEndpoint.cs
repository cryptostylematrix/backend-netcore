using ReferalProgram.Application.Features.Inviters;

namespace ReferalProgram.Presentation.Endpoints.Inviters.GetInviter;

public sealed class GetInviterEndpoint(ISender sender)
    : Endpoint<GetInviterRequest, GetInviterResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/inviter");
        Tags("Program");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get inviter";
            s.Description = "Gets the inviter profile address for a profile in a marketing program.";
            s.ExampleRequest = new GetInviterRequest
            {
                MarketingAddr = "E...",
                ProfileAddr = "E..."
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new GetInviterResponse
            {
                InviterProfileAddr = "E..."
            };
        });
    }

    public override async Task HandleAsync(GetInviterRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetInviterQuery(request.MarketingAddr, request.ProfileAddr),
            ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
