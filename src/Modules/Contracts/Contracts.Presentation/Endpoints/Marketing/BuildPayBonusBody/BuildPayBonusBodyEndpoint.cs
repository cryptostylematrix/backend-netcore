using Contracts.Application.Features.Marketing;

namespace Contracts.Presentation.Endpoints.Marketing.BuildPayBonusBody;


public sealed class BuildPayBonusBodyEndpoint(ISender sender) : 
    Endpoint<BuildPayBonusBodyRequest, PayBonusBodyResponse>
{
    public override void Configure()
    {
        Get("contracts/marketing/body/pay-bonus");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Build Pay Bonus Body";
            s.Description = "Build Pay Bonus Body";
            s.ExampleRequest = new BuildPayBonusBodyRequest
            {
                Key = 123,
                WalletAddr = "E...",
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new PayBonusBodyResponse
            {
                BocHex = "..."
            };
        });
    }

    public override async Task HandleAsync(BuildPayBonusBodyRequest request, CancellationToken ct)
    {
        var query = new BuildPayBonusBodyQuery(
            Key: request.Key,
            WalletAddr: request.WalletAddr);
            
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