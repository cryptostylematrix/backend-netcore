using Contracts.Application.Features.MarketingV3;

namespace Contracts.Presentation.Endpoints.MarketingV3.GetMarketingData;

public sealed class GetMarketingDataEndpoint(ISender sender)
    : Endpoint<GetMarketingDataRequest, MarketingV3DataResponse>
{
    public override void Configure()
    {
        Get("contracts/marketing-v3/{addr}/data");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s => s.Summary = "Get Marketing V3 Data");
    }

    public override async Task HandleAsync(GetMarketingDataRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetMarketingDataQuery(request.Addr), ct);
        if (!result.IsSuccess)
            await Send.ResultAsync(result.ToResult());
        else
            Response = result.Value;
    }
}
