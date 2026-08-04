using ReferalProgram.Application.Features.Structures;

namespace ReferalProgram.Presentation.Endpoints.Structures.GetStructure;

public sealed class GetStructureEndpoint(ISender sender)
    : Endpoint<GetStructureRequest, StructureResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get structure";
            summary.Description = "Gets a referral-program structure by marketing address and structure number.";
        });
    }

    public override async Task HandleAsync(GetStructureRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetStructureQuery(
            request.MarketingAddr,
            request.StructureNumber), ct);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
            return;
        }

        Response = result.Value;
    }
}
