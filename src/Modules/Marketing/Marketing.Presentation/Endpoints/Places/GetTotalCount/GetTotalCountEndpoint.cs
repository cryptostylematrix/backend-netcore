using Marketing.Application.Features.Places;

namespace Marketing.Presentation.Endpoints.Places.GetTotalCount;

public sealed class GetTotalCountEndpoint(ISender sender) : Endpoint<GetTotalCountRequest, PlacesTotalCountResponse>
{
    public override void Configure()
    {
        Get("/api/marketing/{marketing_addr}/places/total-count");
        Tags("Matrix");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Places Count";
            s.Description = "Get Places Count";
            s.ExampleRequest = new GetTotalCountRequest
            {
                MarketingAddr = "E...",
                ProfileAddr = "E...",
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new PlacesCountResponse
            {
                Count = 123
            };
        });
    }

    public override async Task HandleAsync(GetTotalCountRequest request, CancellationToken ct)
    {
        var query = new GetTotalCountQuery(
            MarketingAddr: request.MarketingAddr,
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