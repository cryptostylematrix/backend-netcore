using Microsoft.AspNetCore.Http;
using UI.Application.Features.ProfileIntents;

namespace UI.Presentation.Endpoints.ProfileIntents.GetWalletProfiles;

public sealed class GetWalletProfilesEndpoint(ISender sender)
    : Endpoint<GetWalletProfilesRequest, IReadOnlyCollection<WalletProfileResponse>>
{
    public override void Configure()
    {
        Get("/api/ui/wallets/{wallet_addr}/profiles");
        Tags("UI Profiles");
        AllowAnonymous();
        Summary(summary =>
            summary.Summary = "List a wallet's profile display intents");
    }

    public override async Task HandleAsync(
        GetWalletProfilesRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new GetWalletProfilesQuery(request.WalletAddr),
            ct);
        if (!result.IsSuccess)
        {
            await Send.ResultAsync(Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Could not list wallet profiles",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", result.Errors }
                }));
            return;
        }

        Response = result.Value;
    }
}
