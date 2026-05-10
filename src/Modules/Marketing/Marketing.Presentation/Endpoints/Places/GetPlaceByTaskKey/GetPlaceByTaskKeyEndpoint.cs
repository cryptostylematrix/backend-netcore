using Marketing.Application.Features.Places;

namespace Marketing.Presentation.Endpoints.Places.GetPlaceByTaskKey;


public sealed class GetPlaceByTaskKeyEndpoint(ISender sender) : 
    Endpoint<GetPlaceByTaskKeyRequest, PlaceResponse>
{
    public override void Configure()
    {
        Get("/api/marketing/{marketing_addr}/place-by-task-key");
        Tags("Marketing");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Place by task key";
            s.Description = "Get Place by task key";
            s.ExampleRequest = new GetPlaceByTaskKeyRequest
            {
                MarketingAddr = "E...",
                TaskKey = 123,
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new PlaceResponse
            {
                Addr = "E...", 
                ParentAddr = "E...",
                PlaceNumber = 3,
                CreatedAt  = 123456,
                Pos = 0,
                ProfileLogin = "login",
                M = 3,
                ProfileAddr = "E...",
            };
        });
    }

    public override async Task HandleAsync(GetPlaceByTaskKeyRequest request, CancellationToken ct)
    {
        var query = new GetPlaceByTaskKeyQuery(
            MarketingAddr: request.MarketingAddr,
            TaskKey: request.TaskKey);
        
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