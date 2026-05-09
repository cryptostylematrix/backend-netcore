using Contracts.Application.Features.Marketing;

namespace Contracts.Presentation.Endpoints.Marketing.GetFirstTask;


public sealed class GetFirstTaskEndpoint(ISender sender) : 
    Endpoint<GetFirstTaskRequest, FirstTaskResponse>
{
    public override void Configure()
    {
        Get("contracts/marketing/{addr}/first-task");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get first queue task";
            s.Description = "Get first queue task";
            s.ResponseExamples[StatusCodes.Status200OK] = new FirstTaskResponse
            {
                Key = 123, 
                Val = new MarketingTaskResponse
                {
                    QueryId = 456,
                    M = 3,
                    ProfileAddr = "E..",
                    Payload = new MarketingTaskPayloadResponse
                    {
                        Tag  = 3,
                        SourceAddr = "E...",
                        Pos = new PosDataResponse
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

    public override async Task HandleAsync(GetFirstTaskRequest request, CancellationToken ct)
    {
        var query = new GetFirstTaskQuery(request.Addr);
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