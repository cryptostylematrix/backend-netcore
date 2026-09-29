using System.Text.RegularExpressions;

namespace Contracts.Application.Validation;

public static partial class ProfileLogin
{
    [GeneratedRegex("\\A[a-z0-9][a-z0-9-]{2,18}[a-z0-9]\\z", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    public static bool IsValid(string login) => Pattern().IsMatch(login);
}
