using ReferalProgram.Application.Features.Places;

namespace ReferalProgram.Presentation.Endpoints.Places.GetPath;

public sealed class GetPathEndpoint(ISender sender)
    : Endpoint<GetPathRequest, IReadOnlyCollection<PlaceResponse>>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/path");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get place path";
            summary.Description = "Gets the ancestor path between two places in a referral-program structure.";
        });
    }

    public override async Task HandleAsync(GetPathRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetPathQuery(
            request.MarketingAddr,
            request.StructureNumber,
            request.FromProfileAddr,
            request.FromPlaceNumber,
            request.ToProfileAddr,
            request.ToPlaceNumber), ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
