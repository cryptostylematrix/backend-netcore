using Contracts.Application.Features.JettonWallet;

namespace Contracts.Presentation.Endpoints.JettonWallet.BuildTransferMsgBody;

public sealed class BuildTransferMsgBodyEndpoint(ISender sender)
    : Endpoint<BuildTransferMsgBodyRequest, JettonTransferMsgBodyResponse>
{
    public override void Configure()
    {
        Get("contracts/jetton-wallet/body/transfer");
        Tags("Contracts");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Build Jetton Transfer Message Body";
            summary.Description = "Builds a TEP-74 Jetton transfer body. Amount fields are in their smallest units.";
        });
    }

    public override async Task HandleAsync(BuildTransferMsgBodyRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new BuildTransferMsgBodyQuery(
            request.QueryId,
            request.Amount,
            request.DestinationAddr,
            request.ResponseDestinationAddr,
            request.CustomPayloadBocHex,
            request.ForwardTonAmount,
            request.ForwardPayloadBocHex), ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
