using Contracts.Application.Features.Multi;

namespace Contracts.Presentation.Endpoints.Multi.GetMinQueueTask;

public sealed class GetMinQueueTaskEndpoint(ISender sender) : EndpointWithoutRequest<MinQueueTaskResponse>
{
    public override void Configure()
    {
        Get("contracts/multi/min-queue-task");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a payment";
            s.Description = "Create a payment";
            s.ResponseExamples[StatusCodes.Status200OK] = new MinQueueTaskResponse
            {
                Key = 123, 
                Val = new MultiTaskItemResponse
                {
                    QueryId = 456,
                    M = 3,
                    ProfileAddr = "E..",
                    Payload = new MultiTaskPayloadResponse
                    {
                        Tag  = 3,
                        SourceAddr = "E...",
                        Pos = new PlacePosDataResponse
                        {
                            ParentAddr = "E...",
                            Pos = 1
                        }
                    }
                },
                Flag  = -1
            };
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var query = new GetMinQueueTaskQuery();
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