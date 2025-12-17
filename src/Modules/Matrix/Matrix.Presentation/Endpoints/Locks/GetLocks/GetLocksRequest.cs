namespace Matrix.Presentation.Endpoints.Locks.GetLocks;

public sealed class GetLocksRequest
{
    public int M { get; init; }
    public string ProfileAddr { get; init; } = null!;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}