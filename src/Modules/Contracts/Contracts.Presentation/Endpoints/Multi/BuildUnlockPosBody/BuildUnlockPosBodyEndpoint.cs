using Contracts.Application.Features.Multi;

namespace Contracts.Presentation.Endpoints.Multi.BuildUnlockPosBody;


public sealed class BuildUnlockPosBodyEndpoint(ISender sender) : 
    Endpoint<BuildUnlockPosBodyRequest, UnlockPosBodyResponse>
{
    public override void Configure()
    {
        Get("contracts/multi/body/unlock-pos");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Build Unlock Pos Body";
            s.Description = "Build Unlock Pos Body";
            s.ExampleRequest = new BuildUnlockPosBodyRequest
            {
                M = 2,
                ProfileAddr="E...",
                ParentAddr = "E...",
                Pos = 1
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new UnlockPosBodyResponse
            {
                BocHex = "..."
            };
        });
    }

    public override async Task HandleAsync(BuildUnlockPosBodyRequest request, CancellationToken ct)
    {
        var query = new BuildUnlockPosBodyQuery(
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