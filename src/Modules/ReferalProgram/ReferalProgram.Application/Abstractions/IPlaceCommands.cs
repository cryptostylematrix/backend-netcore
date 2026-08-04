namespace ReferalProgram.Application.Abstractions;

public interface IPlaceCommands
{
    Task<PlaceResponse> CreatePlaceAsync(
        CreatePlaceCommand command,
        CancellationToken cancellationToken);
}

public sealed record CreatePlaceCommand(
    int ParentId,
    uint ParentFilling,
    string MarketingAddr,
    byte StructureNumber,
    string ProfileAddr,
    string ProfileLogin,
    string Index,
    uint PlaceNumber,
    string ParentProfileAddr,
    string? ParentProfileLogin,
    uint ParentPlaceNumber,
    string Mp,
    byte PosGroup,
    byte Kind,
    uint Pos,
    uint Filling,
    uint Deep,
    bool IsActive,
    long CreatedAt,
    long? ActivatedAt,
    uint PersonalVolume,
    uint GroupVolume,
    int TaskKey,
    long TaskQueryId,
    string? TaskSourceAddr);
