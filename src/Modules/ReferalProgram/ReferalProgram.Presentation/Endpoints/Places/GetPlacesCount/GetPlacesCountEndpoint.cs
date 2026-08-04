using ReferalProgram.Application.Features.Places;

namespace ReferalProgram.Presentation.Endpoints.Places.GetPlacesCount;

public sealed class GetPlacesCountEndpoint(ISender sender)
    : Endpoint<GetPlacesCountRequest, PlacesCountResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/places/count");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get profile places count";
            summary.Description = "Gets the number of places owned by a profile in a referral-program structure.";
        });
    }

    public override async Task HandleAsync(GetPlacesCountRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetPlacesCountQuery(
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
