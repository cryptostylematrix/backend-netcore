using Contracts.Application.Features.Marketing;

namespace Contracts.Presentation.Endpoints.Marketing.BuildBuyPlaceByTonBody;

public sealed class BuildBuyPlaceByTonBodyEndpoint(ISender sender) : 
    Endpoint<BuildBuyPlaceByTonBodyRequest, BuyPlaceByTonBodyResponse>
{
    public override void Configure()
    {
        Get("contracts/marketing/body/buy-place-by-ton");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Build Buy Place By TON Body";
            s.Description = "Build Buy Place By TON Body";
            s.ExampleRequest = new BuildBuyPlaceByTonBodyRequest
            {
                M = 2,
                ProfileAddr="E...",
                First = true,
                ParentAddr = "E...",
                Pos = 1
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new BuyPlaceByTonBodyResponse
            {
                BocHex = "..."
            };
        });
    }

    public override async Task HandleAsync(BuildBuyPlaceByTonBodyRequest request, CancellationToken ct)
    {
        var query = new BuildBuyPlaceByTonBodyQuery(
            M: request.M,
            ProfileAddr: request.ProfileAddr,
            First: request.First,
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