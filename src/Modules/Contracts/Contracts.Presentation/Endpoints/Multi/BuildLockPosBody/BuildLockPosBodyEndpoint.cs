using Contracts.Application.Features.Multi;

namespace Contracts.Presentation.Endpoints.Multi.BuildLockPosBody;


public sealed class BuildLockPosBodyEndpoint(ISender sender) : 
    Endpoint<BuildLockPosBodyRequest, LockPosBodyResponse>
{
    public override void Configure()
    {
        Get("contracts/multi/body/lock-pos");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Build Lock Pos Body";
            s.Description = "Build Lock Pos Body";
            s.ExampleRequest = new BuildLockPosBodyRequest
            {
                M = 2,
                ProfileAddr="E...",
                ParentAddr = "E...",
                Pos = 1
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new LockPosBodyResponse
            {
                BocHex = "..."
            };
        });
    }

    public override async Task HandleAsync(BuildLockPosBodyRequest request, CancellationToken ct)
    {
        var query = new BuildLockPosBodyQuery(
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