using Contracts.Application.Features.MarketingV3;

namespace Contracts.Presentation.Endpoints.MarketingV3.BuildExecMessageBody;

public sealed class BuildExecMessageBodyEndpoint(ISender sender)
    : Endpoint<BuildExecMessageBodyRequest, MarketingV3MessageBodyResponse>
{
    public override void Configure()
    {
        Get("contracts/marketing-v3/body/exec");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Build Marketing V3 Exec Message Body";
            s.Description = "Builds the serialized body for a Marketing V3 exec message.";
        });
    }

    public override async Task HandleAsync(BuildExecMessageBodyRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new BuildExecMessageBodyQuery(
            request.QueryId,
            request.Structure,
            request.ProfileAddr,
            request.CommandTag,
            request.PayloadBocHex), ct);

        if (!result.IsSuccess)
            await Send.ResultAsync(result.ToResult());
        else
            Response = result.Value;
    }
}
