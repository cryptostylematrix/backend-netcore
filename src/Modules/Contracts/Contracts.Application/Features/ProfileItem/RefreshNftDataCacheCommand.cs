namespace Contracts.Application.Features.ProfileItem;

public sealed record RefreshNftDataCacheCommand(string Addr) : ICommand;

internal sealed class RefreshNftDataCacheCommandHandler(IProfileItemQueries queries)
    : ICommandHandler<RefreshNftDataCacheCommand>
{
    public Task<Result> Handle(RefreshNftDataCacheCommand request, CancellationToken ct)
        => queries.InvalidateNftDataCacheAsync(request.Addr, ct);
}