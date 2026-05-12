using Contracts.Application.Features.Marketing;

namespace Contracts.Presentation.Endpoints.Marketing.BuildDeployPlaceBody;


public sealed class BuildDeployPlaceBodyEndpoint(ISender sender) : 
    Endpoint<BuildDeployPlaceBodyRequest, DeployPlaceBodyResponse>
{
    public override void Configure()
    {
        Get("contracts/marketing/body/deploy-place");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Build Deploy Place Body";
            s.Description = "Build Deploy Place Body";
            s.ExampleRequest = new BuildDeployPlaceBodyRequest
            {
                Key = 123,
                ParentAddr ="E...",
                Kind = 1,
                ProfileAddr="E...",
                PlaceNumber = 1234,
                InviterProfileAddr ="E...",
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new BuyPlaceByTonBodyResponse
            {
                BocHex = "..."
            };
        });
    }

    public override async Task HandleAsync(BuildDeployPlaceBodyRequest request, CancellationToken ct)
    {
        var query = new BuildDeployPlaceBodyQuery(
            Key: request.Key,
            ParentAddr: request.ParentAddr,
            Kind: request.Kind,
            ProfileAddr: request.ProfileAddr,
            PlaceNumber: request.PlaceNumber,
            InviterProfileAddr: request.InviterProfileAddr);
            
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