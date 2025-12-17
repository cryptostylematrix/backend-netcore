namespace Common.Application;

public sealed class Paginated<T>
{
    public T[] Items { get; init; } = null!;
    public int Page { get; init; }
    public int TotalPages { get; init; }
}