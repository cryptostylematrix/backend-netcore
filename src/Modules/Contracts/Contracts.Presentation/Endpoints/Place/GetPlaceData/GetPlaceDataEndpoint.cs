using Contracts.Application.Features.Place;

namespace Contracts.Presentation.Endpoints.Place.GetPlaceData;

public sealed class GetPlaceDataEndpoint(ISender sender) : Endpoint<GetPlaceDataRequest, PlaceDataResponse>
{
    public override void Configure()
    {
        Get("contracts/place/{addr}/data");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Place data";
            s.Description = "Get Place data";
            s.ExampleRequest = new GetPlaceDataRequest
            {
                Addr = ""
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new PlaceDataResponse
            {
                MarketingAddr = "E...",
                M = 2,
                ParentAddr = "E...",
                CreatedAt = 1234,
                FillCount = 2,
                Profiles = new PlaceProfilesResponse
                {
                    Clone = 1,
                    ProfileAddr = "E...",
                    PlaceNumber = 12,
                    InviterProfileAddr = "E...",
                },
                Security = new PlaceSecurityResponse
                {
                    AdminAddr = "E..."
                },
                Children = new PlaceChildrenResponse
                {
                    LeftAddr = "E...",
                    RightAddr = "E..."
                }
            };
        });
    }

    public override async Task HandleAsync(GetPlaceDataRequest request, CancellationToken ct)
    {
        var query = new GetPlaceDataQuery(request.Addr);
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