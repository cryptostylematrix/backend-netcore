using Marketing.Application.Features.Matrix;

namespace Marketing.Presentation.Endpoints.Matrix.GetNextPos;

public sealed class GetNextPosEndpoint(ISender sender) : Endpoint<GetNextPosRequest, NextPosResponse>
{
    public override void Configure()
    {
        Get("/api/marketing/{marketing_addr}/next-pos");
        Tags("Matrix");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Next Pos";
            s.Description = "Get Next Pos";
            s.ExampleRequest = new GetNextPosRequest
            {
                MarketingAddr = "E...",
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
        var query = new GetNextPosQuery(
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