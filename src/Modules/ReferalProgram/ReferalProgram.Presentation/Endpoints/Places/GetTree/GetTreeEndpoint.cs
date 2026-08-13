using ReferalProgram.Application.Features.Places;

namespace ReferalProgram.Presentation.Endpoints.Places.GetTree;

public sealed class GetTreeEndpoint(ISender sender)
    : Endpoint<GetTreeRequest, TreeNodeResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/tree");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get place tree";
            summary.Description =
                "Gets a place tree using the structure's configured width and height. "
                + "Purchase and lock actions are calculated for viewer_profile_addr; "
                + "viewer_wallet_addr is required for wallet-owned lock actions.";
        });
    }

    public override async Task HandleAsync(GetTreeRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetTreeQuery(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            request.PlaceNumber,
            request.ViewerProfileAddr,
            request.ViewerWalletAddr,
            request.FromPos,
            request.ToPos), ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
