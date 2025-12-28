using Matrix.Application.Features.Locks;

namespace Matrix.Presentation.Endpoints.Locks.GetLocks;

public sealed class GetLocksEndpoint(ISender sender) : Endpoint<GetLocksRequest, Paginated<LockResponse>>
{
    public override void Configure()
    {
        Get("/api/matrix/{M}/{ProfileAddr}/locks");
        Tags("Matrix");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Locks";
            s.Description = "Get Locks";
            s.ExampleRequest = new GetLocksRequest
            {
                ProfileAddr = "E...",
                M = 3,
                Page = 2,
                PageSize = 10,
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new Paginated<LockResponse>
            {
                Page = 2,
                TotalPages = 10,
                Items = [
                    new LockResponse{
                        M = 3,
                        ProfileAddr = "E...",
                        PlaceAddr= "E...", 
                        LockedPos = 1,
                        PlaceProfileLogin = "login",
                        PlaceNumber = 3,
                        CreatedAt  = 123456,
                    }
                ]
            };
        });
    }

    public override async Task HandleAsync(GetLocksRequest request, CancellationToken ct)
    {
        var query = new GetLocksQuery(request.M, request.ProfileAddr, request.Page, request.PageSize);
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