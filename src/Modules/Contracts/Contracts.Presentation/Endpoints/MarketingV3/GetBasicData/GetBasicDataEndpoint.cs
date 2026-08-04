using Contracts.Application.Features.MarketingV3;

namespace Contracts.Presentation.Endpoints.MarketingV3.GetBasicData;

public sealed class GetBasicDataEndpoint(ISender sender)
    : Endpoint<GetBasicDataRequest, MarketingV3BasicDataResponse>
{
    public override void Configure()
    {
        Get("contracts/marketing-v3/{addr}/basic-data");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s => s.Summary = "Get Marketing V3 Basic Data");
    }

    public override async Task HandleAsync(GetBasicDataRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetBasicDataQuery(request.Addr), ct);
        if (!result.IsSuccess)
            await Send.ResultAsync(result.ToResult());
        else
            Response = result.Value;
    }
}
