using Common.Domain;

namespace ReferalProgram.Core.PlaceAggregate;

public sealed class Place : Entity, IAggregateRoot
{
    private Place()
    {
    }

    private Place(
        int parentId,
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        string? profileLogin,
        string index,
        uint placeNumber,
        string? parentProfileAddr,
        string? parentProfileLogin,
        uint parentPlaceNumber,
        string mp,
        byte posGroup,
        byte kind,
        uint pos,
        uint filling,
        uint deep,
        bool isActive,
        long createdAt,
        long? activatedAt,
        uint personalVolume,
        uint groupVolume)
    {
        ParentId = parentId;
        MarketingAddr = marketingAddr;
        StructureNumber = structureNumber;
        ProfileAddr = profileAddr;
        ProfileLogin = profileLogin;
        Index = index;
        PlaceNumber = placeNumber;
        ParentProfileAddr = parentProfileAddr;
        ParentProfileLogin = parentProfileLogin;
        ParentPlaceNumber = parentPlaceNumber;
        Mp = mp;
        PosGroup = posGroup;
        Kind = kind;
        Pos = pos;
        Filling = filling;
        Deep = deep;
        IsActive = isActive;
        CreatedAt = createdAt;
        ActivatedAt = activatedAt;
        PersonalVolume = personalVolume;
        GroupVolume = groupVolume;
        MatrixFilling = 1;
    }

    public int Id { get; private set; }
    public int? ParentId { get; private set; }
    public string Mp { get; private set; } = null!;
    public byte PosGroup { get; private set; }
    public string MarketingAddr { get; private set; } = null!;
    public byte StructureNumber { get; private set; }
    public string? ProfileAddr { get; private set; }
    public uint PlaceNumber { get; private set; }
    public string? ProfileLogin { get; private set; }
    public string Index { get; private set; } = null!;
    public string? ParentProfileAddr { get; private set; }
    public string? ParentProfileLogin { get; private set; }
    public uint? ParentPlaceNumber { get; private set; }
    public long CreatedAt { get; private set; }
    public long? ActivatedAt { get; private set; }
    public bool IsActive { get; private set; }
    public byte Kind { get; private set; }
    public uint Pos { get; private set; }
    public uint Filling { get; private set; }
    public uint Deep { get; private set; }
    public uint PersonalVolume { get; private set; }
    public uint GroupVolume { get; private set; }
    public long MatrixFilling { get; private set; } = 1;

    public static Place Create(
        int parentId,
        string marketingAddr,
        byte structureNumber,
        string? profileAddr,
        string? profileLogin,
        string index,
        uint placeNumber,
        string? parentProfileAddr,
        string? parentProfileLogin,
        uint parentPlaceNumber,
        string mp,
        byte posGroup,
        byte kind,
        uint pos,
        uint filling,
        uint deep,
        bool isActive,
        long createdAt,
        long? activatedAt,
        uint personalVolume,
        uint groupVolume)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(marketingAddr);
        ArgumentException.ThrowIfNullOrWhiteSpace(index);
        ArgumentException.ThrowIfNullOrWhiteSpace(mp);

        if ((profileAddr is null) != (profileLogin is null))
        {
            throw new InvalidOperationException(
                "Profile address and profile login must either both be set or both be null.");
        }

        if (profileAddr is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(profileAddr);
            ArgumentException.ThrowIfNullOrWhiteSpace(profileLogin);
        }

        var place = new Place(
            parentId,
            marketingAddr,
            structureNumber,
            profileAddr,
            profileLogin,
            index,
            placeNumber,
            parentProfileAddr,
            parentProfileLogin,
            parentPlaceNumber,
            mp,
            posGroup,
            kind,
            pos,
            filling,
            deep,
            isActive,
            createdAt,
            activatedAt,
            personalVolume,
            groupVolume);

        place.AddPlaceCreatedDomainEvent();

        return place;
    }

    public static Place Buy(
        int parentId,
        string marketingAddr,
        byte structureNumber,
        string profileAddr,
        string profileLogin,
        string index,
        uint placeNumber,
        string? parentProfileAddr,
        string? parentProfileLogin,
        uint parentPlaceNumber,
        string mp,
        byte posGroup,
        byte kind,
        uint pos,
        uint deep,
        long boughtAt)
    {
        var place = Create(
            parentId,
            marketingAddr,
            structureNumber,
            profileAddr,
            profileLogin,
            index,
            placeNumber,
            parentProfileAddr,
            parentProfileLogin,
            parentPlaceNumber,
            mp,
            posGroup,
            kind,
            pos,
            filling: 0,
            deep,
            isActive: true,
            createdAt: boughtAt,
            activatedAt: boughtAt,
            personalVolume: 0,
            groupVolume: 0);

        place.EnsureBoughtEffects();

        return place;
    }

    public static Place BuySystem(
        int parentId,
        string marketingAddr,
        byte structureNumber,
        string index,
        uint placeNumber,
        string? parentProfileAddr,
        string? parentProfileLogin,
        uint parentPlaceNumber,
        string mp,
        byte posGroup,
        byte kind,
        uint pos,
        uint deep,
        long boughtAt)
    {
        return Create(
            parentId,
            marketingAddr,
            structureNumber,
            profileAddr: null,
            profileLogin: null,
            index,
            placeNumber,
            parentProfileAddr,
            parentProfileLogin,
            parentPlaceNumber,
            mp,
            posGroup,
            kind,
            pos,
            filling: 0,
            deep,
            isActive: true,
            createdAt: boughtAt,
            activatedAt: boughtAt,
            personalVolume: 0,
            groupVolume: 0);
    }

    private void AddPlaceCreatedDomainEvent()
    {
        if (Pos == 0)
            throw new InvalidOperationException("A child place position must be greater than zero.");

        AddDomainEvent(new PlaceCreatedDomainEvent(
            ParentId!.Value,
            expectedParentFilling: checked(Pos - 1)));
    }

    public void EnsureBoughtEffects()
    {
        if (string.IsNullOrWhiteSpace(ProfileAddr))
            throw new InvalidOperationException("A bought place must have a profile address.");

        AddDomainEvent(new PlaceBoughtDomainEvent(
            MarketingAddr,
            StructureNumber,
            ProfileAddr,
            PlaceNumber,
            ActivatedAt ?? CreatedAt));
    }

    public void RecordProcessedMarketingCommand(
        int taskKey,
        long taskQueryId,
        string? taskSourceAddr,
        Place responseSourcePlace,
        uint responseCode,
        DateTimeOffset processedAt)
    {
        AddDomainEvent(new MarketingCommandProcessedDomainEvent(
            MarketingAddr,
            taskKey,
            taskQueryId,
            taskSourceAddr,
            this,
            responseSourcePlace,
            responseCode,
            processedAt));
    }

    public void RegisterChild(uint expectedFilling)
    {
        if (Kind == PlaceKinds.TerminalClone)
        {
            throw new InvalidOperationException(
                "A terminal clone cannot have children.");
        }

        if (Filling != expectedFilling)
            throw new InvalidOperationException("The parent place changed while creating a place.");

        Filling = checked(Filling + 1);
    }

    public void IncreasePersonalVolume()
    {
        PersonalVolume = checked(PersonalVolume + 1);
    }

    public void Activate(long activatedAt)
    {
        ActivatedAt ??= activatedAt;
        IsActive = true;
    }
}
