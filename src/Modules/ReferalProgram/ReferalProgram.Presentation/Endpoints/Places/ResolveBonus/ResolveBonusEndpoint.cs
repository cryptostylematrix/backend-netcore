using ReferalProgram.Application.Features.Places;

namespace ReferalProgram.Presentation.Endpoints.Places.ResolveBonus;

public sealed class ResolveBonusEndpoint(ISender sender)
    : Endpoint<ResolveBonusRequest, BonusResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/resolve-bonus");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Resolve bonus";
            summary.Description = "Resolves the reason place and recipient profile for a referral-program bonus.";
        });
    }

    public override async Task HandleAsync(ResolveBonusRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ResolveBonusQuery(
            request.MarketingAddr,
            request.BonusTypeTag,
            request.StructureNumber,
            request.RelativeProfileAddr,
            request.RelativePlaceNumber,
            request.Level), ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
