namespace ProgramPlaceOwnerChanger;

public sealed record OwnerChangeRequest(
    string Marketing, short? Structure, string OldAddress, string NewAddress, string NewLogin)
{
    public static short? ParseStructure(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!short.TryParse(value, out var number) || number is < 1 or > 255)
            throw new ArgumentException("Structure must be between 1 and 255; referral structure 0 cannot be transferred.");
        return number;
    }
}
