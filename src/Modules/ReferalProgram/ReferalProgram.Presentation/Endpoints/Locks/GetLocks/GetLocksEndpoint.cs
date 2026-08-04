using ReferalProgram.Application.Features.Locks;

namespace ReferalProgram.Presentation.Endpoints.Locks.GetLocks;

public sealed class GetLocksEndpoint(ISender sender)
    : Endpoint<GetLocksRequest, Paginated<LockResponse>>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/locks");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get profile locks";
            summary.Description = "Gets a profile's locks in a referral-program structure.";
        });
    }

    public override async Task HandleAsync(GetLocksRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetLocksQuery(
            request.MarketingAddr,
            request.StructNumber,
            request.ProfileAddr,
            request.Page,
            request.PageSize), ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
