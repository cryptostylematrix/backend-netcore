namespace Contracts.Application.Features.ProfileItem;

public sealed record BuildEditContentBodyQuery(
    string Login,
    string? ImageUrl,
    string? FirstName,
    string? LastName,
    string? TgUsername): IQuery<EditContentBodyResponse>;
    
    
internal sealed class BuildEditContentBodyQueryHandler(IProfileItemQueries queries)
    : IQueryHandler<BuildEditContentBodyQuery, EditContentBodyResponse>
{
    public Task<Result<EditContentBodyResponse>> Handle(BuildEditContentBodyQuery request, CancellationToken ct)
        => Task.FromResult(queries.BuildEditContentBody(
            queryId: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            login: request.Login,
            imageUrl: request.ImageUrl,
            firstName: request.FirstName,
            lastName: request.LastName,
            tgUsername: request.TgUsername));
}