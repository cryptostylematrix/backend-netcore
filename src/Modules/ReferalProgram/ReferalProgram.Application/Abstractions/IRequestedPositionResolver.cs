namespace ReferalProgram.Application.Abstractions;

public sealed record RequestedPositionResolution(
    NextPosResponse? Position,
    string? Reason)
{
    public bool IsSuccess => Position is not null;
}

public interface IRequestedPositionResolver
{
    Task<RequestedPositionResolution> ResolveAsync(
        string marketingAddr,
        byte structureNumber,
        byte structureWidth,
        byte positionGroup,
        RequestedPosition requestedPosition,
        string? requiredRootMp,
        IReadOnlyCollection<string> lockMps,
        CancellationToken cancellationToken);
}
