using Contracts.Application.Features.Marketing;

namespace Contracts.Presentation.Endpoints.Marketing.BuildBuyPlaceByJettonBody;


public sealed class BuildBuyPlaceByJettonBodyEndpoint(ISender sender) : 
    Endpoint<BuildBuyPlaceByJettonBodyRequest, BuyPlaceByJettonBodyResponse>
{
    public override void Configure()
    {
        Get("contracts/marketing/body/buy-place-by-jetton");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Build Buy Place By Jetton Body";
            s.Description = "Build Buy Place By Jetton Body";
            s.ExampleRequest = new BuildBuyPlaceByJettonBodyRequest
            {
                M = 2,
                ProfileAddr="E...",
                First = true,
                ParentAddr = "E...",
                Pos = 1,
                
                MarketingAddr = "E...",
                Amount = 150000,
                SenderAddr = "E...",
                Fee = 50000000
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new BuyPlaceByTonBodyResponse
            {
                BocHex = "..."
            };
        });
    }

    public override async Task HandleAsync(BuildBuyPlaceByJettonBodyRequest request, CancellationToken ct)
    {
        var query = new BuildBuyPlaceByJettonBodyQuery(
            MarketingAddr: request.MarketingAddr,
            M: request.M,
            ProfileAddr: request.ProfileAddr,
            First: request.First,
            ParentAddr: request.ParentAddr,
            Pos: request.Pos,
            Amount: request.Amount,
            SenderAddr: request.SenderAddr,
            Fee: request.Fee);
            
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