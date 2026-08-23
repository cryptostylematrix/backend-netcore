namespace ReferalProgram.Application.Abstractions;

public interface IClonePlaceKindPolicy
{
    Task<byte> ResolveAsync(
        PositionSelection selection,
        int parentId,
        CancellationToken cancellationToken);
}
