namespace ReferalProgram.Application.Abstractions;

public static class ProgramCommandTags
{
    public const uint BuyFirstPlace = 0xd7dd1e7a;
    public const uint BuyPlace = 0xb070143f;
    public const uint BuyTopPlace = 0x3f6eb1fa;
    public const uint LockPosition = 0x6292cd93;
    public const uint UnlockPosition = 0xcc64122d;
}

public enum BuyPlaceKind
{
    First,
    Regular,
    Top
}
