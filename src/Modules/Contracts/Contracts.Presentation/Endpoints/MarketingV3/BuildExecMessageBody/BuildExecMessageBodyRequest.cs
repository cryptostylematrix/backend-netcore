namespace Contracts.Presentation.Endpoints.MarketingV3.BuildExecMessageBody;

public sealed class BuildExecMessageBodyRequest
{
    [BindFrom("query_id")]
    public ulong QueryId { get; init; }

    [BindFrom("structure")]
    public byte Structure { get; init; }

    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;

    [BindFrom("command_tag")]
    public uint CommandTag { get; init; }

    [BindFrom("payload_boc_hex")]
    public string? PayloadBocHex { get; init; }
}
