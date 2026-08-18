using UI.Application.Features.ProfileIntents;

namespace UI.Presentation.Endpoints.ProfileIntents.AddProfileIntent;

public sealed class AddProfileIntentEndpoint(ISender sender)
    : Endpoint<AddProfileIntentRequest, ProfileIntentOperationResponse>
{
    public override void Configure()
    {
        Post("/api/ui/wallets/{wallet_addr}/profiles");
        Tags("UI Profiles");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Add a profile display intent for a wallet";
            summary.Description =
                "Owner mode verifies on-chain ownership. Preview mode is available "
                + "for every valid profile.";
        });
    }

    public override async Task HandleAsync(
        AddProfileIntentRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new AddProfileIntentCommand(
            request.WalletAddr,
            request.Login,
            request.Mode), ct);
        Response = result.Value;
    }
}
