using Common.Domain;
using UI.Core.WalletProfileIntentAggregate.Events;

namespace UI.Core.WalletProfileIntentAggregate;

public sealed class WalletProfileIntent : Entity, IAggregateRoot
{
    private WalletProfileIntent()
    {
    }

    private WalletProfileIntent(
        string walletAddr,
        string profileAddr,
        WalletProfileMode mode,
        bool owned,
        DateTime createdAtUtc)
    {
        WalletAddr = walletAddr;
        ProfileAddr = profileAddr;
        Mode = mode;
        Owned = owned;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public long Id { get; private set; }
    public string WalletAddr { get; private set; } = null!;
    public string ProfileAddr { get; private set; } = null!;
    public WalletProfileMode Mode { get; private set; }
    public bool Owned { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static WalletProfileIntent Add(
        string walletAddr,
        string profileAddr,
        WalletProfileMode mode,
        bool owned,
        DateTime addedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(walletAddr);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileAddr);
        var occurredAtUtc = EnsureUtc(addedAtUtc);
        var intent = new WalletProfileIntent(
            walletAddr.Trim(),
            profileAddr.Trim(),
            mode,
            owned,
            occurredAtUtc);

        intent.AddDomainEvent(new WalletProfileIntentAddedDomainEvent(
            Guid.NewGuid(),
            occurredAtUtc,
            intent.WalletAddr,
            intent.ProfileAddr,
            mode,
            owned));

        return intent;
    }

    public void UpdateOwnership(bool owned, DateTime checkedAtUtc)
    {
        var occurredAtUtc = EnsureUtc(checkedAtUtc);
        if (Owned == owned)
            return;

        var ownershipWasLost = Owned && !owned;
        var ownershipWasGained = !Owned && owned;
        Owned = owned;
        UpdatedAtUtc = occurredAtUtc;

        if (ownershipWasLost)
        {
            AddDomainEvent(new WalletProfileOwnershipLostDomainEvent(
                Guid.NewGuid(),
                occurredAtUtc,
                WalletAddr,
                ProfileAddr,
                Mode));
        }
        else if (ownershipWasGained)
        {
            AddDomainEvent(new WalletProfileOwnershipGainedDomainEvent(
                Guid.NewGuid(),
                occurredAtUtc,
                WalletAddr,
                ProfileAddr,
                Mode));
        }
    }

    public void NormalizeWalletAddress(string walletAddr, DateTime normalizedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(walletAddr);
        var normalized = walletAddr.Trim();
        if (string.Equals(WalletAddr, normalized, StringComparison.Ordinal))
            return;

        WalletAddr = normalized;
        UpdatedAtUtc = EnsureUtc(normalizedAtUtc);
    }

    public void Remove(DateTime removedAtUtc)
    {
        var occurredAtUtc = EnsureUtc(removedAtUtc);
        UpdatedAtUtc = occurredAtUtc;
        AddDomainEvent(new WalletProfileIntentRemovedDomainEvent(
            Guid.NewGuid(),
            occurredAtUtc,
            WalletAddr,
            ProfileAddr,
            Mode,
            Owned));
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
