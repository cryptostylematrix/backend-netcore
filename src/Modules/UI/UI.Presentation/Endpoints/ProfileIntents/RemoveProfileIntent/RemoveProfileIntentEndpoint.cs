using UI.Application.Features.ProfileIntents;

namespace UI.Presentation.Endpoints.ProfileIntents.RemoveProfileIntent;

public sealed class RemoveProfileIntentEndpoint(ISender sender)
    : Endpoint<RemoveProfileIntentRequest, ProfileIntentOperationResponse>
{
    public override void Configure()
    {
        Delete("/api/ui/wallets/{wallet_addr}/profiles/{login}");
        Tags("UI Profiles");
        AllowAnonymous();
        Summary(summary =>
            summary.Summary = "Remove a profile display intent from a wallet");
    }

    public override async Task HandleAsync(
        RemoveProfileIntentRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new RemoveProfileIntentCommand(
            request.WalletAddr,
            request.Login), ct);
        Response = result.Value;
    }
}
