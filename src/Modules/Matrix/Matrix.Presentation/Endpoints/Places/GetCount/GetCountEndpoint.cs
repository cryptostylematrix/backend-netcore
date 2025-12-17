using Matrix.Application.Features.Places;

namespace Matrix.Presentation.Endpoints.Places.GetCount;


public sealed class GetCountEndpoint(ISender sender) : Endpoint<GetCountRequest, PlacesCountResponse>
{
    public override void Configure()
    {
        Get("/api/matrix/{M}/{ProfileAddr}/places/count");
        Tags("Matrix");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Places Count";
            s.Description = "Get Places Count";
            s.ExampleRequest = new GetCountRequest
            {
                ProfileAddr = "E...",
                M = 3
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new PlacesCountResponse
            {
               Count = 123
            };
        });
    }

    public override async Task HandleAsync(GetCountRequest request, CancellationToken ct)
    {
        var query = new GetCountQuery(request.M, request.ProfileAddr);
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