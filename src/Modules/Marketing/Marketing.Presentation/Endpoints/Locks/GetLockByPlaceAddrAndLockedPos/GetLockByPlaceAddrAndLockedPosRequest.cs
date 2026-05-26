namespace Marketing.Presentation.Endpoints.Locks.GetLockByPlaceAddrAndLockedPos;

public sealed class GetLockByPlaceAddrAndLockedPosRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;

    [BindFrom("place_addr")]
    public string PlaceAddr { get; init; } = null!;

    [BindFrom("locked_pos")]
    public int LockedPos { get; init; }

    [BindFrom("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
}