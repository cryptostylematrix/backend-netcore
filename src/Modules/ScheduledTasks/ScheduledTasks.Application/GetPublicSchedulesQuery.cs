using Common.Application;
using Ardalis.Result;
using ScheduledTasks.Application.Abstractions;

namespace ScheduledTasks.Application;

public sealed record GetPublicSchedulesQuery(string Module, string Scope, string ResourceType) : IQuery<IReadOnlyList<PublicSchedule>>;

public sealed class GetPublicSchedulesQueryHandler(IPublicScheduleQueries schedules)
    : IQueryHandler<GetPublicSchedulesQuery, IReadOnlyList<PublicSchedule>>
{
    public async Task<Result<IReadOnlyList<PublicSchedule>>> Handle(GetPublicSchedulesQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Module) || string.IsNullOrWhiteSpace(request.Scope)
            || string.IsNullOrWhiteSpace(request.ResourceType))
            return Result<IReadOnlyList<PublicSchedule>>.Invalid(new ValidationError("Module, scope and resource type are required."));
        return Result.Success(await schedules.GetAsync(new(request.Module.Trim(), request.Scope.Trim(), request.ResourceType.Trim()), ct));
    }
}
