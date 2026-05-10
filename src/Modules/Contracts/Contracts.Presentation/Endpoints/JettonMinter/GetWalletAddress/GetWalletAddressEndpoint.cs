using Contracts.Application.Features.JettonMinter;

namespace Contracts.Presentation.Endpoints.JettonMinter.GetWalletAddress;

public sealed class GetWalletAddressEndpoint(ISender sender) : 
    Endpoint<GetWalletAddressRequest, JettonWalletAddressResponse>
{
    public override void Configure()
    {
        Get("contracts/jetton-minter/{addr}/wallet-addr");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Jetton Wallet Address";
            s.Description = "Get Jetton Wallet Address";
            s.ResponseExamples[StatusCodes.Status200OK] = new JettonWalletAddressResponse
            {
                WalletAddr = "E..",
            };
        });
    }

    public override async Task HandleAsync(GetWalletAddressRequest request, CancellationToken ct)
    {
        var query = new GetWalletAddressQuery(
            Addr: request.Addr, 
            OwnerAddr: request.OwnerAddr);
        
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