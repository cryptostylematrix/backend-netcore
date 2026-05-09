using Marketing.Application.Features.Places;

namespace Marketing.Presentation.Endpoints.Places.SearchPaces;

public sealed class SearchPacesEndpoint(ISender sender) : Endpoint<SearchPacesRequest, Paginated<PlaceResponse>>
{
    public override void Configure()
    {
        Get("/api/marketing/{marketing_addr}/search");
        Tags("Matrix");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Search Places";
            s.Description = "Search Places";
            s.ExampleRequest = new SearchPacesRequest
            {
                MarketingAddr = "E...",
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
        var query = new SearchPacesQuery(
            MarketingAddr: request.MarketingAddr,
            M: request.M, 
            ProfileAddr: request.ProfileAddr, 
            Page: request.Page, 
            PageSize: request.PageSize, 
            Query: request.Query);
        
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