using Matrix.Application.Features.Places;

namespace Matrix.Presentation.Endpoints.Places.GetPlaces;

public sealed class GetPlacesEndpoint(ISender sender) : Endpoint<GetPlacesRequest, Paginated<PlaceResponse>>
{
    public override void Configure()
    {
        Get("/api/matrix/{M}/{ProfileAddr}/places");
        Tags("Matrix");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Places";
            s.Description = "Get Places";
            s.ExampleRequest = new GetPlacesRequest
            {
                ProfileAddr = "E...",
                M = 3,
                Page = 2,
                PageSize = 10,
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new Paginated<PlaceResponse>
            {
                Page = 2,
                TotalPages = 10,
                Items = [
                    new PlaceResponse{
                        Addr = "E...", 
                        ParentAddr = "E...",
                        PlaceNumber = 3,
                        CreatedAt  = 123456,
                        FillCount = 3,
                        Clone = 1,
                        Pos = 0,
                        Login = "login",
                        M = 3,
                        ProfileAddr = "E...",
                    }
                ]
            };
        });
    }

    public override async Task HandleAsync(GetPlacesRequest request, CancellationToken ct)
    {
        var query = new GetPlacesQuery(request.M, request.ProfileAddr, request.Page, request.PageSize);
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