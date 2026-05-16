namespace Marketing.Presentation.Endpoints.Places.GetPlaceByTaskKey;

public sealed class GetPlaceByTaskKeyRequest
{
    [BindFrom("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
    
    [BindFrom("task_key")]
    public int TaskKey { get; init; }
}