using Contracts.Application.Features.Marketing;

namespace Contracts.Presentation.Endpoints.Marketing.GetPlaceAddress;


public sealed class GetPlaceAddressEndpoint(ISender sender) : 
    Endpoint<GetPlaceAddressRequest, PlaceAddressResponse>
{
    public override void Configure()
    {
        Get("contracts/marketing/{marketing_addr}/place-addr");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Place address";
            s.Description = "Get Place address";
            s.ResponseExamples[StatusCodes.Status200OK] = new PlaceAddressResponse
            {
                Addr = "E..."
            };
        });
    }

    public override async Task HandleAsync(GetPlaceAddressRequest request, CancellationToken ct)
    {
        var query = new GetPlaceAddressQuery(
            MarketingAddr: request.MarketingAddr,
            M: request.M,
            ParentAddr: request.ParentAddr,
            Pos: request.Pos);
        
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