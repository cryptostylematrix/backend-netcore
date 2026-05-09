using Contracts.Application.Features.MatrixPlace;

namespace Contracts.Presentation.Endpoints.Place.GetMatrixPlaceData;

public sealed class GetMatrixPlaceDataEndpoint(ISender sender) : Endpoint<GetMatrixPlaceDataRequest, MatrixPlaceDataResponse>
{
    public override void Configure()
    {
        Get("contracts/matrix-place/{addr}/data");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Place data";
            s.Description = "Get Place data";
            s.ExampleRequest = new GetMatrixPlaceDataRequest
            {
                Addr = ""
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new MatrixPlaceDataResponse
            {
                Init = false,
                MarketingAddr = "E...",
                M = 2,
                ParentAddr = "E...",
                Pos = 3,
                SeqNo = 14,
                Width = 0,
                Height = 1,
                AdminAddr = "E...",
                Info = new PlaceInfoResponse
                {
                    Kind = 1,
                    ProfileAddr = "E...",
                    PlaceNumber = 12,
                    InviterProfileAddr = "E...",
                },
                Descendants =
                {
                    
                }
            };
        });
    }

    public override async Task HandleAsync(GetMatrixPlaceDataRequest request, CancellationToken ct)
    {
        var query = new GetMatrixPlaceDataQuery(request.Addr);
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