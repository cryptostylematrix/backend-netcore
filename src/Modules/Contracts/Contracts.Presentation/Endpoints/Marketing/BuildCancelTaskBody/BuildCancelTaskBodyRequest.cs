namespace Contracts.Presentation.Endpoints.Marketing.BuildCancelTaskBody;

public sealed class BuildCancelTaskBodyRequest
{
    [BindFrom("query_id")]
    public ulong QueryId { get; init; }
    
    [BindFrom("key")]
    public uint Key { get; init; }
    
    [BindFrom("comment")]
    public string Comment { get; init; } = null!;
}