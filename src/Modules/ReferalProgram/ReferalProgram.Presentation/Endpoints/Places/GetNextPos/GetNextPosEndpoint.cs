using ReferalProgram.Application.Features.Places;

namespace ReferalProgram.Presentation.Endpoints.Places.GetNextPos;

public sealed class GetNextPosEndpoint(ISender sender)
    : Endpoint<GetNextPosRequest, NextPosResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/next-pos");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get next position";
            summary.Description = "Gets the next available position in a referral-program structure.";
        });
    }

    public override async Task HandleAsync(GetNextPosRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetNextPosQuery(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr), ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
