using ReferalProgram.Application.Features.ProfileVolumes;

namespace ReferalProgram.Presentation.Endpoints.ProfileVolumes.GetProfileVolume;

public sealed class GetProfileVolumeEndpoint(ISender sender)
    : Endpoint<GetProfileVolumeRequest, ProfileVolumeResponse>
{
    public override void Configure()
    {
        Get("/api/program/{marketing_addr}/structures/{structure_number}/volume");
        Tags("Program");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "Get profile volume";
            summary.Description =
                "Gets personal, referral, and group volume for one profile in a structure. "
                + "Missing volume is returned as zero.";
        });
    }

    public override async Task HandleAsync(
        GetProfileVolumeRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetProfileVolumeQuery(
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
