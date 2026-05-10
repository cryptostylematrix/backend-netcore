using Marketing.Application.Features.Places;

namespace Marketing.Presentation.Endpoints.Places.GetMaxPlaceNumber;

public sealed class GetMaxPlaceNumberEndpoint(ISender sender)
    : Endpoint<GetMaxPlaceNumberRequest, uint>
{
    public override void Configure()
    {
        Get("/api/marketing/{marketing_addr}/max-place-number");

        Tags("Marketing");

        AllowAnonymous();

        Summary(s =>
        {
            s.Summary = "Get max place number";
            s.Description = "Returns the maximum place number for profile in matrix.";

            s.ExampleRequest = new GetMaxPlaceNumberRequest
            {
                MarketingAddr = "E...",
                M = 3,
                ProfileAddr = "E..."
            };

            s.ResponseExamples[StatusCodes.Status200OK] = 123u;
        });
    }

    public override async Task HandleAsync(
        GetMaxPlaceNumberRequest request,
        CancellationToken ct)
    {
        var query = new GetMaxPlaceNumberQuery(
            MarketingAddr: request.MarketingAddr,
            M: request.M,
            ProfileAddr: request.ProfileAddr);

        var result = await sender.Send(query, ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
        }
        else
        {
            Response = result.Value;
        }
    }
}