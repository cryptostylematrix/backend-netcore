namespace CryptoStyle.Api.BackgroundServices;

public sealed class TaskProcessorOptions
{
    public const string SectionName = "TaskProcessor";

    public int IntervalSeconds { get; init; } = 5;
}
