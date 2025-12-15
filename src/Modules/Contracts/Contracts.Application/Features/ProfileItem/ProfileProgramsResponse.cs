namespace Contracts.Application.Features.ProfileItem;

public sealed class ProfileProgramsResponse
{
    public ProgramDataResponse? Multi { get; init; }
}

public sealed class ProgramDataResponse
{
    public string InviterAddr { get; init; } = null!;
    public uint SeqNo { get; init; }
    public string InviteAddr { get; init; } = null!;
    public uint Confirmed { get; init; }
}