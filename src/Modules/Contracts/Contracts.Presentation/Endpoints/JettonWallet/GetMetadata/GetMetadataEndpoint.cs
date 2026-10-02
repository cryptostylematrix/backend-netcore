using Contracts.Application.Features.JettonWallet;

namespace Contracts.Presentation.Endpoints.JettonWallet.GetMetadata;

public sealed class GetMetadataRequest
{
    public string Addr { get; init; } = "";
}

public sealed class GetMetadataEndpoint(ISender sender)
    : Endpoint<GetMetadataRequest, JettonMetadataResponse>
{
    public override void Configure()
    {
        Get("contracts/jetton-wallet/{addr}/metadata");
        Tags("Contracts");
        AllowAnonymous();
        Summary(summary => summary.Summary = "Resolve and cache Jetton name, symbol and decimals");
    }

    public override async Task HandleAsync(GetMetadataRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetMetadataQuery(request.Addr), ct);
        if (!result.IsSuccess) await Send.ResultAsync(result.ToResult());
        else Response = result.Value;
    }
}
