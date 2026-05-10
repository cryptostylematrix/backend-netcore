using Contracts.Application.Features.JettonWallet;

namespace Contracts.Presentation.Endpoints.JettonWallet.GetWalletData;


public sealed class GetWalletDataEndpoint(ISender sender) : 
    Endpoint<GetWalletDataRequest, JettonWalletDataResponse>
{
    public override void Configure()
    {
        Get("contracts/jetton-wallet/{addr}/data");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Jetton Wallet Data";
            s.Description = "Get Jetton Wallet Data";
            s.ResponseExamples[StatusCodes.Status200OK] = new JettonWalletDataResponse
            {
                Balance = 1234,
                OwnerAddr = "E..",
                MinterAddr = "E..",
            };
        });
    }

    public override async Task HandleAsync(GetWalletDataRequest request, CancellationToken ct)
    {
        var query = new GetWalletDataQuery(
            Addr: request.Addr);
        
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