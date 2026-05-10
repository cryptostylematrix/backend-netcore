using Marketing.Application.Features.Locks;

namespace Marketing.Presentation.Endpoints.Locks.GetLockByPlaceAddrAndLockedPos;

public sealed class GetLockByPlaceAddrAndLockedPosEndpoint(ISender sender)
    : Endpoint<GetLockByPlaceAddrAndLockedPosRequest, LockResponse>
{
    public override void Configure()
    {
        Get("/api/marketing/{marketing_addr}/lock");

        Tags("Marketing");

        AllowAnonymous();

        Summary(s =>
        {
            s.Summary = "Get lock by place address and locked position";

            s.Description =
                "Returns lock information for a specific locked position.";

            s.ExampleRequest = new GetLockByPlaceAddrAndLockedPosRequest
            {
                MarketingAddr = "E...",
                PlaceAddr = "E...",
                LockedPos = 1,
                ProfileAddr = "E..."
            };

            s.ResponseExamples[StatusCodes.Status200OK] = new LockResponse
            {
                MarketingAddr = "E...",
                M = 3,
                ProfileAddr = "E...",
                PlaceAddr = "E...",
                LockedPos = 1,
                PlaceProfileLogin = "login",
                PlaceNumber = 10,
                CreatedAt = 123456
            };
        });
    }

    public override async Task HandleAsync(
        GetLockByPlaceAddrAndLockedPosRequest request,
        CancellationToken ct)
    {
        var query = new GetLockByPlaceAddrAndLockedPosQuery(
            MarketingAddr: request.MarketingAddr,
            PlaceAddr: request.PlaceAddr,
            LockedPos: request.LockedPos,
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