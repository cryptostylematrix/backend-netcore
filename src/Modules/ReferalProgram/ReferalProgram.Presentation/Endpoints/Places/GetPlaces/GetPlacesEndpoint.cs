using ReferalProgram.Application.Features.Places;

namespace ReferalProgram.Presentation.Endpoints.Places.GetPlaces;

public sealed class GetPlacesEndpoint(ISender sender)
    : Endpoint<GetPlacesRequest, Paginated<PlaceWithMatrixResponse>>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/places");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get profile places";
            summary.Description = "Gets paginated profile places with matrix size and filling. Set only_not_closed=true to exclude full matrices.";
        });
    }

    public override async Task HandleAsync(GetPlacesRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetPlacesQuery(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            request.Page,
            request.PageSize,
            request.OnlyNotClosed), ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
