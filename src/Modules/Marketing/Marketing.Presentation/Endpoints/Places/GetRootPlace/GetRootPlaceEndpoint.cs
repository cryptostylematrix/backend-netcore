using Marketing.Application.Features.Places;

namespace Marketing.Presentation.Endpoints.Places.GetRootPlace;

public sealed class GetRootPlaceEndpoint(ISender sender) : Endpoint<GetRootPlaceRequest, PlaceResponse>
{
    public override void Configure()
    {
        Get("/api/marketing/{marketing_addr}/root");
        Tags("Matrix");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Root Place";
            s.Description = "Get Root Place";
            s.ExampleRequest = new GetRootPlaceRequest
            {
                MarketingAddr = "E...",
                ProfileAddr = "E...",
                M = 3
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

    public override async Task HandleAsync(GetRootPlaceRequest request, CancellationToken ct)
    {
        var query = new GetRootPlaceQuery(
            MarketingAddr: request.MarketingAddr,
            M: request.M,
            ProfileAddr: request.ProfileAddr);
        
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