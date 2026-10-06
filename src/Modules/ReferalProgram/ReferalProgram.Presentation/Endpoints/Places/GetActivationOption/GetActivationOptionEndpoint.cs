using ReferalProgram.Application.Features.Places;

namespace ReferalProgram.Presentation.Endpoints.Places.GetActivationOption;

public sealed class GetActivationOptionEndpoint(ISender sender)
    : Endpoint<GetActivationOptionRequest, ActivationOptionResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/activation-option");
        Tags("Program");
        AllowAnonymous();
        Summary(s => s.Summary = "Get activation target and eligibility for a place's activity source");
    }

    public override async Task HandleAsync(GetActivationOptionRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetActivationOptionQuery(request.MarketingAddr,
            request.StructureNumber, request.ProfileAddr, request.PlaceNumber), ct);
        if (!result.IsSuccess) { await Send.ResultAsync(result.ToResult()); return; }
        Response = result.Value;
    }
}
