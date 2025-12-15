namespace Contracts.Application.Features.Invite;

public sealed class InviteDataResponse
{
    public string AdminAddr { get; init; } = null!;
    public uint Program { get; init; }
    public uint NextRefNo { get; init; }
    public uint Number { get; init; }
    public string? ParentAddr { get; init; }
    public InviteOwnerResponse? Owner { get; init; }
}

public sealed class InviteOwnerResponse
{
    public string OwnerAddr { get; init; } = null!;
    public ulong SetAt { get; init; }
}
