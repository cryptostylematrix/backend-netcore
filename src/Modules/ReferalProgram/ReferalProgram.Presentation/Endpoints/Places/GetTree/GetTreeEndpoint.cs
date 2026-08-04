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
            summary.Description = "Gets a place tree using the structure's configured width and height.";
        });
    }

    public override async Task HandleAsync(GetTreeRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetTreeQuery(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            request.PlaceNumber,
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
