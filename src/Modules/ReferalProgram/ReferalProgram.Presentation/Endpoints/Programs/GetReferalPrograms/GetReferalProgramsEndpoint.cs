using ReferalProgram.Application.Features.Programs;

namespace ReferalProgram.Presentation.Endpoints.Programs.GetReferalPrograms;

public sealed class GetReferalProgramsEndpoint(ISender sender)
    : EndpointWithoutRequest<IReadOnlyCollection<ReferalProgramResponse>>
{
    public override void Configure()
    {
        Get("/api/programs");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "List referral programs";
            summary.Description = "Gets all configured referral programs.";
            summary.ResponseExamples[StatusCodes.Status200OK] = new[]
            {
                new ReferalProgramResponse
                {
                    MarketingAddr = "E..."
                }
            };
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(new GetReferalProgramsQuery(), ct);
        Response = result.Value;
    }
}
