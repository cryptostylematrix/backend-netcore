using Matrix.Application.Features.Places;

namespace Matrix.Presentation.Endpoints.Places.SearchPaces;


public sealed class SearchPacesEndpoint(ISender sender) : Endpoint<SearchPacesRequest, Paginated<PlaceResponse>>
{
    public override void Configure()
    {
        Get("/api/matrix/{M}/{ProfileAddr}/search");
        Tags("Matrix");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Search Places";
            s.Description = "Search Places";
            s.ExampleRequest = new SearchPacesRequest
            {
                ProfileAddr = "E...",
                M = 3,
                Page = 2,
                PageSize = 10,
                Query = "query"
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
                        Filling2 = 3,
                        Clone = 1,
                        Pos = 0,
                        ProfileLogin = "login",
                        M = 3,
                        ProfileAddr = "E...",
                    }
                ]
            };
        });
    }

    public override async Task HandleAsync(SearchPacesRequest request, CancellationToken ct)
    {
        var query = new SearchPacesQuery(request.M, request.ProfileAddr, request.Page, request.PageSize, request.Query);
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