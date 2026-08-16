namespace ProgramMigrator;

internal enum LegacyProgramType
{
    Multi,
    Neo
}

internal enum MigrationScope
{
    Invite,
    Structures
}

internal sealed record PlaceMigrationNode(
    string SourceKey,
    string? ParentSourceKey,
    string? SourceAddr,
    byte StructureNumber,
    string ProfileAddr,
    string ProfileLogin,
    uint PlaceNumber,
    long CreatedAt,
    byte Kind,
    uint Pos,
    uint Filling,
    uint Deep,
    string Mp);

internal sealed record LockMigrationNode(
    byte StructureNumber,
    string PlaceProfileAddr,
    uint PlaceNumber,
    string PlaceProfileLogin,
    string ProfileAddr,
    uint LockedPos,
    string Mp,
    long CreatedAt);

internal sealed record PendingInvite(
    string InviteAddr,
    string? ParentInviteAddr,
    uint Pos,
    string Mp,
    uint Deep);

internal sealed record ProgramMigrationData(
    IReadOnlyList<PlaceMigrationNode> Places,
    IReadOnlyList<LockMigrationNode> Locks);
