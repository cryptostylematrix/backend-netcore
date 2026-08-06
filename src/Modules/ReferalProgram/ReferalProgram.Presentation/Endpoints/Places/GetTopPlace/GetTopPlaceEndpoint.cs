using ReferalProgram.Application.Features.Places;

namespace ReferalProgram.Presentation.Endpoints.Places.GetTopPlace;

public sealed class GetTopPlaceEndpoint(ISender sender)
    : Endpoint<GetTopPlaceRequest, PlaceResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/top-place");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get top place";
            summary.Description = "Gets the top-most place in a referral-program structure.";
        });
    }

    public override async Task HandleAsync(GetTopPlaceRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetTopPlaceQuery(
            request.MarketingAddr,
            request.StructureNumber), ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
