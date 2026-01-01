using Contracts.Application.Features.ProfileItem;

namespace Contracts.Presentation.Endpoints.ProfileItem.BuildEditContentBody;


public sealed class BuildEditContentBodyEndpoint(ISender sender) : 
    Endpoint<BuildEditContentBodyRequest, EditContentBodyResponse>
{
    public override void Configure()
    {
        Get("contracts/profile-item/body/edit-content");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Build Edit Content Body";
            s.Description = "Build Edit Content Body";
            s.ExampleRequest = new BuildEditContentBodyRequest
            {
                Login = "login",
                ImageUrl="https://www.example.com/image.png",
                FirstName = "John",
                LastName = "Doe",
                TgUsername = "@john_doe"
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new EditContentBodyResponse
            {
                BocHex = "..."
            };
        });
    }

    public override async Task HandleAsync(BuildEditContentBodyRequest request, CancellationToken ct)
    {
        var query = new BuildEditContentBodyQuery(
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