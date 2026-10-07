using System.Text.Json.Serialization;
using UI.Application.Features.WalletPreferences;

namespace UI.Presentation.Endpoints.WalletPreferences;

public sealed class WalletLanguageRequest
{
    [BindFrom("wallet_addr")]
    public string WalletAddr { get; init; } = "";
    [JsonPropertyName("language")]
    public string? Language { get; init; }
}

public sealed class ResolveWalletLanguageEndpoint(ISender sender)
    : Endpoint<WalletLanguageRequest, WalletLanguageResponse>
{
    public override void Configure()
    {
        Post("/api/ui/wallets/{wallet_addr}/language/resolve");
        AllowAnonymous();
        Tags("UI Preferences");
        Summary(s => s.Summary = "Load saved language, initializing it from the supplied language only if absent");
    }
    public override async Task HandleAsync(WalletLanguageRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SaveWalletLanguageCommand(request.WalletAddr, request.Language, false), ct);
        await Send.OkAsync(result.Value, ct);
    }
}

public sealed class SaveWalletLanguageEndpoint(ISender sender)
    : Endpoint<WalletLanguageRequest, WalletLanguageResponse>
{
    public override void Configure()
    {
        Put("/api/ui/wallets/{wallet_addr}/language");
        AllowAnonymous();
        Tags("UI Preferences");
        Summary(s => s.Summary = "Save an explicitly selected wallet language");
    }
    public override async Task HandleAsync(WalletLanguageRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SaveWalletLanguageCommand(request.WalletAddr, request.Language, true), ct);
        await Send.OkAsync(result.Value, ct);
    }
}
