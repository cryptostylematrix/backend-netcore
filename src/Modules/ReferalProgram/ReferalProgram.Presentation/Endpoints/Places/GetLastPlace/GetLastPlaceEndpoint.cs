using ReferalProgram.Application.Features.Places;

namespace ReferalProgram.Presentation.Endpoints.Places.GetLastPlace;

public sealed class GetLastPlaceEndpoint(ISender sender)
    : Endpoint<GetLastPlaceRequest, PlaceResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/last-place");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get last place";
            summary.Description = "Gets the highest-numbered place for a profile in a referral-program structure.";
        });
    }

    public override async Task HandleAsync(GetLastPlaceRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetLastPlaceQuery(
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
