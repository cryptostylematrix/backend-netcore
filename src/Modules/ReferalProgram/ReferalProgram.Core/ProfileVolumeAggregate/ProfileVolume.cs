using Common.Domain;

namespace ReferalProgram.Core.ProfileVolumeAggregate;

public sealed class ProfileVolume : Entity, IAggregateRoot
{
    private ProfileVolume()
    {
    }

    public string MarketingAddr { get; private set; } = null!;
    public byte StructureNumber { get; private set; }
    public string ProfileAddr { get; private set; } = null!;
    public uint PersonalVolume { get; private set; }
    public uint ReferralVolume { get; private set; }
    public uint GroupVolume { get; private set; }
}
