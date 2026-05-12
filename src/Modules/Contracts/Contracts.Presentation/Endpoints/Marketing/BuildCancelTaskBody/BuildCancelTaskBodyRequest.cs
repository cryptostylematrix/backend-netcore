namespace Contracts.Presentation.Endpoints.Marketing.BuildCancelTaskBody;

public sealed class BuildCancelTaskBodyRequest
{
    [BindFrom("key")]
    public uint Key { get; init; }
    
    [BindFrom("comment")]
    public string Comment { get; init; } = null!;
}