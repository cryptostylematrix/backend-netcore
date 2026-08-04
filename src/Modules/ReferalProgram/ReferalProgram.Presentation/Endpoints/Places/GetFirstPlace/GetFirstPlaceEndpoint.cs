using ReferalProgram.Application.Features.Places;

namespace ReferalProgram.Presentation.Endpoints.Places.GetFirstPlace;

public sealed class GetFirstPlaceEndpoint(ISender sender)
    : Endpoint<GetFirstPlaceRequest, PlaceResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/first-place");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get first place";
            summary.Description = "Gets the lowest-numbered place for a profile in a referral-program structure.";
        });
    }

    public override async Task HandleAsync(GetFirstPlaceRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetFirstPlaceQuery(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr), ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
