using UI.Application.Features.ProfileIntents;

namespace UI.Presentation.Endpoints.ProfileIntents.CheckWalletProfiles;

public sealed class CheckWalletProfilesEndpoint(ISender sender)
    : Endpoint<CheckWalletProfilesRequest, CheckWalletProfilesResponse>
{
    public override void Configure()
    {
        Post("/api/ui/wallets/{wallet_addr}/profiles/check");
        Tags("UI Profiles");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Refresh a wallet's profile display intents";
            summary.Description =
                "Rechecks ownership and profile content for every stored profile.";
        });
    }

    public override async Task HandleAsync(
        CheckWalletProfilesRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new CheckWalletProfilesCommand(request.WalletAddr),
            ct);
        Response = result.Value;
    }
}
