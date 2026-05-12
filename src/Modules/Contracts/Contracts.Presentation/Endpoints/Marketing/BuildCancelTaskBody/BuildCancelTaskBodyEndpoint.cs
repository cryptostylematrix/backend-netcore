using Contracts.Application.Features.Marketing;

namespace Contracts.Presentation.Endpoints.Marketing.BuildCancelTaskBody;


public sealed class BuildCancelTaskBodyEndpoint(ISender sender) : 
    Endpoint<BuildCancelTaskBodyRequest, CancelTaskBodyResponse>
{
    public override void Configure()
    {
        Get("contracts/marketing/body/cancel-task");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Build Cancel Task Body";
            s.Description = "Build Cancel Task Body";
            s.ExampleRequest = new BuildCancelTaskBodyRequest
            {
                Key = 123,
                Comment = "the reason",
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new CancelTaskBodyResponse
            {
                BocHex = "..."
            };
        });
    }

    public override async Task HandleAsync(BuildCancelTaskBodyRequest request, CancellationToken ct)
    {
        var query = new BuildCancelTaskBodyQuery(
            Key: request.Key,
            Comment: request.Comment);
            
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