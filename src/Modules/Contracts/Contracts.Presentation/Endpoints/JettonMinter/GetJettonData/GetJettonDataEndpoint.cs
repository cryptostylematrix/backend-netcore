using Contracts.Application.Features.JettonMinter;

namespace Contracts.Presentation.Endpoints.JettonMinter.GetJettonData;

public sealed class GetJettonDataEndpoint(ISender sender)
    : Endpoint<GetJettonDataRequest, JettonMinterDataResponse>
{
    public override void Configure()
    {
        Get("contracts/jetton-minter/{addr}/data");
        Tags("Contracts");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get Jetton Minter Data";
            summary.Description = "Invokes get_jetton_data on a Jetton minter.";
            summary.ResponseExamples[StatusCodes.Status200OK] = new JettonMinterDataResponse
            {
                TotalSupply = "1000000",
                Mintable = true,
                AdminAddress = "EQ...",
                MetadataUri = "https://example.com/jetton.json",
                Decimals = 6,
                ContentBocHex = "b5ee9c72...",
                WalletCodeBocHex = "b5ee9c72..."
            };
        });
    }

    public override async Task HandleAsync(
        GetJettonDataRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetJettonDataQuery(request.Addr), ct);

        if (!result.IsSuccess)
            await Send.ResultAsync(result.ToResult());
        else
            Response = result.Value;
    }
}
