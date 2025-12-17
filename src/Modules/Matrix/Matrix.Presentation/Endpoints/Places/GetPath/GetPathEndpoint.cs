using Matrix.Application.Features.Places;

namespace Matrix.Presentation.Endpoints.Places.GetPath;


public sealed class GetPathEndpoint(ISender sender) : Endpoint<GetPathRequest, IEnumerable<PlaceResponse>>
{
    public override void Configure()
    {
        Get("/api/matrix/path");
        Tags("Matrix");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Path";
            s.Description = "Get Path";
            s.ExampleRequest = new GetPathRequest
            {
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
                    FillCount = 3,
                    Clone = 1,
                    Pos = 0,
                    Login = "login",
                    M = 3,
                    ProfileAddr = "E...",
                },

                new PlaceResponse
                {
                    Addr = "E...",
                    ParentAddr = "E...",
                    PlaceNumber = 3,
                    CreatedAt = 123456,
                    FillCount = 3,
                    Clone = 1,
                    Pos = 0,
                    Login = "login",
                    M = 3,
                    ProfileAddr = "E...",
                },
            };
        });
    }

    public override async Task HandleAsync(GetPathRequest request, CancellationToken ct)
    {
        var query = new GetPathQuery(request.RootAddr, request.PlaceAddr);
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