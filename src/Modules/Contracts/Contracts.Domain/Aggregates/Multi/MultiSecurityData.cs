namespace Contracts.Domain.Aggregates.Multi;

public sealed class MultiSecurityData
{
    public Address Admin { get; init; } = null!;

    public static MultiSecurityData FromCell(Cell cell)
    {
        var slice = cell.Parse();
        return new MultiSecurityData
        {
            Admin = slice.LoadAddress()!
        };
    }
}