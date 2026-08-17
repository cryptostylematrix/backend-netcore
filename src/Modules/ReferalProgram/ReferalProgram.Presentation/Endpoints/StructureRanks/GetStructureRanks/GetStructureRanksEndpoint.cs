using ReferalProgram.Application.Features.StructureRanks;

namespace ReferalProgram.Presentation.Endpoints.StructureRanks.GetStructureRanks;

public sealed class GetStructureRanksEndpoint(ISender sender)
    : Endpoint<GetStructureRanksRequest, IReadOnlyCollection<StructureRankResponse>>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/ranks");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "List structure ranks";
            summary.Description =
                "Gets the ranks configured for a referral-program structure, "
                + "ordered by the required number of active referral places.";
            summary.ResponseExamples[StatusCodes.Status200OK] = new[]
            {
                new StructureRankResponse
                {
                    MarketingAddr = "E...",
                    StructureNumber = 1,
                    Name = "Bronze",
                    RequiredActiveReferralPlaces = 10
                }
            };
        });
    }

    public override async Task HandleAsync(GetStructureRanksRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetStructureRanksQuery(
            request.MarketingAddr,
            request.StructureNumber), ct);

        Response = result.Value;
    }
}
