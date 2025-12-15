namespace Matrix.Application.Features.Places;

public sealed class Paginated<T>
{
    T[] Items { get; set; } = null!;
    public int Page { get; private set; }
    public int TotalPages { get; private set; }
}