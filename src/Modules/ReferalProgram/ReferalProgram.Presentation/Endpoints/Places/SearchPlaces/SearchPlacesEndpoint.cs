using ReferalProgram.Application.Features.Places;

namespace ReferalProgram.Presentation.Endpoints.Places.SearchPlaces;

public sealed class SearchPlacesEndpoint(ISender sender)
    : Endpoint<SearchPlacesRequest, Paginated<PlaceResponse>>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/places/search");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Search places";
            summary.Description =
                "Searches profile logins within the positioning root resolved for viewer_profile_addr.";
        });
    }

    public override async Task HandleAsync(SearchPlacesRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SearchPlacesQuery(
            request.MarketingAddr,
            request.StructureNumber,
            request.ViewerProfileAddr,
            request.Query,
            request.Page,
            request.PageSize), ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
