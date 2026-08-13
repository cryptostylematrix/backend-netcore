using ReferalProgram.Application.Abstractions;
using TonSdk.Core;

namespace ReferalProgram.Infrastructure.Services;

internal sealed class TonAddressComparer : ITonAddressComparer
{
    public bool AreEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        try
        {
            return new Address(left.Trim()).Equals(new Address(right.Trim()));
        }
        catch
        {
            return false;
        }
    }
}
