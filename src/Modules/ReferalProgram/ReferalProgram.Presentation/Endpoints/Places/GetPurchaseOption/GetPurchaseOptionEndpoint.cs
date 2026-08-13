using ReferalProgram.Application.Features.Places;

namespace ReferalProgram.Presentation.Endpoints.Places.GetPurchaseOption;

public sealed class GetPurchaseOptionEndpoint(ISender sender)
    : Endpoint<GetPurchaseOptionRequest, PurchaseOptionResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/purchase-option");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get purchase option";
            summary.Description =
                "Gets the authoritative buy command and position eligibility for a profile.";
        });
    }

    public override async Task HandleAsync(
        GetPurchaseOptionRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetPurchaseOptionQuery(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            request.ParentProfileAddr,
            request.ParentPlaceNumber,
            request.Position), ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
