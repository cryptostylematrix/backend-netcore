using Microsoft.AspNetCore.Http;
using UI.Application.Features.Avatars;

namespace UI.Presentation.Endpoints.Avatars;

public sealed class GetAvatarRequest
{
    public string Login { get; set; } = "";
}

public sealed class GetAvatarEndpoint(ISender sender) : Endpoint<GetAvatarRequest>
{
    public override void Configure()
    {
        Get("/avatar", "/api/ui/avatar");
        AllowAnonymous();
        Tags("UI Profiles");
        Summary(summary => summary.Summary = "Generate a deterministic Profile NFT avatar from login");
    }

    public override async Task HandleAsync(GetAvatarRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetAvatarQuery(request.Login), ct);
        if (!result.IsSuccess)
        {
            await Send.ResultAsync(Results.Problem(statusCode: 400,
                title: "Invalid profile login",
                detail: "Use 4–20 letters, digits or hyphens, starting and ending with a letter or digit."));
            return;
        }

        HttpContext.Response.Headers.CacheControl = "public,max-age=86400";
        HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
        await Send.ResultAsync(Results.Text(result.Value, "image/svg+xml", System.Text.Encoding.UTF8));
    }
}
