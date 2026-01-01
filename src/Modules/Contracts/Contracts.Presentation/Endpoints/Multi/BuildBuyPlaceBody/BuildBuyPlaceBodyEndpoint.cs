using Contracts.Application.Features.Multi;

namespace Contracts.Presentation.Endpoints.Multi.BuildBuyPlaceBody;


public sealed class BuildBuyPlaceBodyEndpoint(ISender sender) : 
    Endpoint<BuildBuyPlaceBodyRequest, BuyPlaceBodyResponse>
{
    public override void Configure()
    {
        Get("contracts/multi/body/buy-place");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Build Buy Place Body";
            s.Description = "Build Buy Place Body";
            s.ExampleRequest = new BuildBuyPlaceBodyRequest
            {
                M = 2,
                ProfileAddr="E...",
                ParentAddr = "E...",
                Pos = 1
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new BuyPlaceBodyResponse
            {
                BocHex = "..."
            };
        });
    }

    public override async Task HandleAsync(BuildBuyPlaceBodyRequest request, CancellationToken ct)
    {
        var query = new BuildBuyPlaceBodyQuery(
            M: request.M,
            ProfileAddr: request.ProfileAddr,
            ParentAddr: request.ParentAddr,
            Pos: request.Pos);
            
        var result = await sender.Send(query, ct);
        
        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
        }
        else
        {
            Response = result.Value;
        }
    }
}