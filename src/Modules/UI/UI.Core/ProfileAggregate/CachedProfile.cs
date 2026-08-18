using Common.Domain;
using UI.Core.ProfileAggregate.Events;

namespace UI.Core.ProfileAggregate;

public sealed class CachedProfile : Entity, IAggregateRoot
{
    private CachedProfile()
    {
    }

    private CachedProfile(
        string address,
        string login,
        string contentJson,
        DateTime updatedAtUtc)
    {
        Address = address;
        Login = login;
        ContentJson = contentJson;
        UpdatedAtUtc = updatedAtUtc;
    }

    public string Address { get; private set; } = null!;
    public string Login { get; private set; } = null!;
    public string ContentJson { get; private set; } = null!;
    public DateTime UpdatedAtUtc { get; private set; }

    public static CachedProfile Create(
        string address,
        string login,
        string contentJson,
        DateTime updatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(login);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentJson);

        return new CachedProfile(
            address.Trim(),
            login.Trim().ToLowerInvariant(),
            contentJson,
            EnsureUtc(updatedAtUtc));
    }

    public bool Refresh(string login, string contentJson, DateTime changedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(login);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentJson);

        var normalizedLogin = login.Trim().ToLowerInvariant();
        if (string.Equals(Login, normalizedLogin, StringComparison.Ordinal)
            && string.Equals(ContentJson, contentJson, StringComparison.Ordinal))
        {
            return false;
        }

        Login = normalizedLogin;
        ContentJson = contentJson;
        UpdatedAtUtc = EnsureUtc(changedAtUtc);
        AddDomainEvent(new ProfileContentChangedDomainEvent(
            Guid.NewGuid(),
            UpdatedAtUtc,
            Address));

        return true;
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
