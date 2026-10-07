using System.Text.Json.Serialization;
using UI.Application.Features.TonConnections;

namespace UI.Presentation.Endpoints.TonConnections;

public sealed class SaveTonConnectionRequest
{
    [BindFrom("wallet_addr")]
    public string WalletAddr { get; init; } = "";
    [JsonPropertyName("wallet_state_init")]
    public string? WalletStateInit { get; init; }
    [JsonPropertyName("wallet_name")]
    public string? WalletName { get; init; }
    [JsonPropertyName("app_version")]
    public string? AppVersion { get; init; }
    [JsonPropertyName("platform")]
    public string? Platform { get; init; }
}

public sealed class SaveTonConnectionEndpoint(ISender sender)
    : Endpoint<SaveTonConnectionRequest, TonConnectionResponse>
{
    public override void Configure()
    {
        Put("/api/ui/wallets/{wallet_addr}/ton-connection");
        AllowAnonymous();
        Tags("UI TonConnections");
        Summary(s => s.Summary = "Save the latest client-reported TON Connect metadata for a wallet");
    }

    public override async Task HandleAsync(SaveTonConnectionRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SaveTonConnectionCommand(request.WalletAddr,
            request.WalletStateInit, request.WalletName, request.AppVersion, request.Platform), ct);
        await Send.OkAsync(result.Value, ct);
    }
}
