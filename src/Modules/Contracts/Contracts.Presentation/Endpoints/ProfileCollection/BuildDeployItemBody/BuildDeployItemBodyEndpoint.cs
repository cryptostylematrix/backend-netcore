using Contracts.Application.Features.ProfileCollection;

namespace Contracts.Presentation.Endpoints.ProfileCollection.BuildDeployItemBody;


public sealed class BuildDeployItemBodyEndpoint(ISender sender) : 
    Endpoint<BuildDeployItemBodyRequest, DeployItemBodyResponse>
{
    public override void Configure()
    {
        Get("contracts/profile-collection/body/deploy-item-content");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Build Deploy Item Content Body";
            s.Description = "Build Deploy Item Content Body";
            s.ExampleRequest = new BuildDeployItemBodyRequest
            {
                Login = "login",
                ImageUrl="https://www.example.com/image.png",
                FirstName = "John",
                LastName = "Doe",
                TgUsername = "@john_doe"
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new DeployItemBodyResponse
            {
                BocHex = "..."
            };
        });
    }

    public override async Task HandleAsync(BuildDeployItemBodyRequest request, CancellationToken ct)
    {
        var query = new BuildDeployItemBodyQuery(
            Login: request.Login,
            ImageUrl: request.ImageUrl,
            FirstName: request.FirstName,
            LastName: request.LastName,
            TgUsername: request.TgUsername);
            
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