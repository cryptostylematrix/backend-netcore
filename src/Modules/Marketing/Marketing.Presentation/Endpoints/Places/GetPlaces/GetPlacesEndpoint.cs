using Marketing.Application.Features.Places;

namespace Marketing.Presentation.Endpoints.Places.GetPlaces;

public sealed class GetPlacesEndpoint(ISender sender) : Endpoint<GetPlacesRequest, Paginated<PlaceResponse>>
{
    public override void Configure()
    {
        Get("/api/marketing/{marketing_addr}/places");
        Tags("Matrix");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Places";
            s.Description = "Get Places";
            s.ExampleRequest = new GetPlacesRequest
            {
                MarketingAddr = "E...",
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
                        Pos = 0,
                        ProfileLogin = "login",
                        M = 3,
                        ProfileAddr = "E...",
                    }
                ]
            };
        });
    }

    public override async Task HandleAsync(GetPlacesRequest request, CancellationToken ct)
    {
        var query = new GetPlacesQuery(
            MarketingAddr: request.MarketingAddr,
            M: request.M, 
            ProfileAddr: request.ProfileAddr, 
            Page: request.Page, 
            PageSize: request.PageSize);
        
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