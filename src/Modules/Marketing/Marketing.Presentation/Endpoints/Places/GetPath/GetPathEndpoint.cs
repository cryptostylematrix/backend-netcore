using Marketing.Application.Features.Places;

namespace Marketing.Presentation.Endpoints.Places.GetPath;

public sealed class GetPathEndpoint(ISender sender) : Endpoint<GetPathRequest, IEnumerable<PlaceResponse>>
{
    public override void Configure()
    {
        Get("/api/marketing/{marketing_addr}/path");
        Tags("Matrix");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Path";
            s.Description = "Get Path";
            s.ExampleRequest = new GetPathRequest
            {
                MarketingAddr = "E...",
                RootAddr = "E...",
                PlaceAddr = "E...",
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new[]
            {

                new PlaceResponse
                {
                    Addr = "E...",
                    ParentAddr = "E...",
                    PlaceNumber = 3,
                    CreatedAt = 123456,
                    Pos = 0,
                    ProfileLogin = "login",
                    M = 3,
                    ProfileAddr = "E...",
                },

                new PlaceResponse
                {
                    Addr = "E...",
                    ParentAddr = "E...",
                    PlaceNumber = 3,
                    CreatedAt = 123456,
                    Pos = 0,
                    ProfileLogin = "login",
                    M = 3,
                    ProfileAddr = "E...",
                },
            };
        });
    }

    public override async Task HandleAsync(GetPathRequest request, CancellationToken ct)
    {
        var query = new GetPathQuery(
            MarketingAddr: request.MarketingAddr,
            RootAddr: request.RootAddr, 
            PlaceAddr: request.PlaceAddr);
        
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