using Contracts.Application.Features.ProfileItem;

namespace Contracts.Presentation.Endpoints.ProfileItem.GetPrograms;


public sealed class GetProgramsEndpoint(ISender sender) : 
    Endpoint<GetProgramsRequest, ProfileProgramsResponse>
{
    public override void Configure()
    {
        Get("contracts/profile-item/{addr}/programs");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Profile Item Programs";
            s.Description = "Get Profile Item Programs";
            s.ExampleRequest = new GetProgramsRequest
            {
                Addr = "E...",
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new ProfileProgramsResponse
            {
                Multi = new ProgramDataResponse
                {
                    InviterAddr = "E...",
                    SeqNo = 23,
                    InviteAddr =  "E...",
                    Confirmed = 1
                }
            };
        });
    }

    public override async Task HandleAsync(GetProgramsRequest request, CancellationToken ct)
    {
        var query = new GetProgramsQuery(request.Addr);
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