using Contracts.Application.Features.ProfileCollection;

namespace Contracts.Presentation.Endpoints.ProfileCollection.GetCollectionData;


public sealed class GetCollectionDataEndpoint(ISender sender) : EndpointWithoutRequest<CollectionDataResponse>
{
    public override void Configure()
    {
        Get("contracts/profile-collection/data");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Profile Collection Data";
            s.Description = "Get Profile Collection Data";
            s.ResponseExamples[StatusCodes.Status200OK] = new CollectionDataResponse
            {
                Addr = "E...",
                OwnerAddr = "E...",
            };
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var query = new GetCollectionDataQuery();
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