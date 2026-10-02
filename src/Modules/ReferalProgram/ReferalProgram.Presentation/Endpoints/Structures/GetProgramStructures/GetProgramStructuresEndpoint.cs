using ReferalProgram.Application.Features.Structures;

namespace ReferalProgram.Presentation.Endpoints.Structures.GetProgramStructures;

public sealed class GetProgramStructuresRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = "";
}

public sealed class GetProgramStructuresEndpoint(ISender sender)
    : Endpoint<GetProgramStructuresRequest, IReadOnlyList<StructureResponse>>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures");
        Tags("Program");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetProgramStructuresRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetProgramStructuresQuery(request.MarketingAddr), ct);
        if (!result.IsSuccess) { await Send.ResultAsync(result.ToResult()); return; }
        Response = result.Value;
    }
}
