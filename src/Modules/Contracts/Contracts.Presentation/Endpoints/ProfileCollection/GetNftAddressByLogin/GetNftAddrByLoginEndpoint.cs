using Contracts.Application.Features.ProfileCollection;

namespace Contracts.Presentation.Endpoints.ProfileCollection.GetNftAddressByLogin;

public sealed class GetNftAddrByLoginEndpoint(ISender sender) : 
    Endpoint<GetNftAddrByLoginRequest, NftAddressResponse>
{
    public override void Configure()
    {
        Get("contracts/profile-collection/nft-addr-by-login/{login}");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get NFT Addr by Login";
            s.Description = "Get NFT Addr by Login";
            s.ExampleRequest = new GetNftAddrByLoginRequest
            {
                Login = "login",
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new NftAddressResponse
            {
                Addr = "E..."
            };
        });
    }

    public override async Task HandleAsync(GetNftAddrByLoginRequest request, CancellationToken ct)
    {
        var query = new GetNftAddressByLoginQuery(request.Login);
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