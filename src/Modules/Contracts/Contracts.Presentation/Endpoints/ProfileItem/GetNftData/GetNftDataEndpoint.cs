using Contracts.Application.Features.ProfileItem;

namespace Contracts.Presentation.Endpoints.ProfileItem.GetNftData;

public sealed class GetNftDataEndpoint(ISender sender) : 
    Endpoint<GetNftDataRequest, ProfileDataResponse>
{
    public override void Configure()
    {
        Get("contracts/profile-item/{addr}/nft-data");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Profile Item NFT Data";
            s.Description = "Get Profile Item NFT Data";
            s.ExampleRequest = new GetNftDataRequest
            {
                Addr = "E...",
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new ProfileDataResponse
            {
                IsInit  = -1,
                Index = "250",
                CollectionAddr = "E...",
                OwnerAddr = "E...",
                Content = new ProfileContentResponse
                {
                    Login = "login",
                    ImageUrl = "image url",
                    FirstName = "first name",
                    LastName = "last name",
                    TgUsername = "tg username",
                }
            };
        });
    }

    public override async Task HandleAsync(GetNftDataRequest request, CancellationToken ct)
    {
        var query = new GetNftDataQuery(request.Addr);
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