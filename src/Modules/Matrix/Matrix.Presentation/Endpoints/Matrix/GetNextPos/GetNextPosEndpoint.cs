using Matrix.Application.Features.Matrix;

namespace Matrix.Presentation.Endpoints.Matrix.GetNextPos;


public sealed class GetNextPosEndpoint(ISender sender) : Endpoint<GetNextPosRequest, NextPosResponse>
{
    public override void Configure()
    {
        Get("/api/matrix/{M}/{ProfileAddr}/next-pos");
        Tags("Matrix");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Next Pos";
            s.Description = "Get Next Pos";
            s.ExampleRequest = new GetNextPosRequest
            {
                ProfileAddr = "E...",
                M = 3
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new NextPosResponse
            {
                ParentAddr = "E...",
                Pos = 0,
            };
        });
    }

    public override async Task HandleAsync(GetNextPosRequest request, CancellationToken ct)
    {
        var query = new GetNextPosQuery(request.M, request.ProfileAddr);
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