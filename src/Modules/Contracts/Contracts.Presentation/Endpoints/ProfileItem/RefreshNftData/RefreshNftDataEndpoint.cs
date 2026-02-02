
using Contracts.Application.Features.ProfileItem;

namespace Contracts.Presentation.Endpoints.ProfileItem.RefreshNftData;

public sealed class RefreshNftDataEndpoint(ISender sender) : 
    Endpoint<RefreshNftDataRequest>
{
    public override void Configure()
    {
        Delete("contracts/profile-item/{addr}/nft-data");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Refresh Profile Item NFT Data";
            s.Description = "Refresh Profile Item NFT Data";
            s.ExampleRequest = new RefreshNftDataRequest
            {
                Addr = "E...",
            };
        });
    }

    public override async Task HandleAsync(RefreshNftDataRequest request, CancellationToken ct)
    {
        var command = new RefreshNftDataCacheCommand(request.Addr);
        var result = await sender.Send(command, ct);
        
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