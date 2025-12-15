namespace Contracts.Domain.Aggregates.Multi;

public sealed class MultiFeesData
{
    public Coins M1 { get; init; } = null!;
    public Coins M2 { get; init; } = null!;
    public Coins M3 { get; init; } = null!;
    public Coins M4 { get; init; } = null!;
    public Coins M5 { get; init; } = null!;
    public Coins M6 { get; init; } = null!;


    public static MultiFeesData FromCell(Cell cell)
    {
        var slice = cell.Parse();
        return new MultiFeesData
        {
            M1 = slice.LoadCoins(),
            M2 = slice.LoadCoins(),
            M3 = slice.LoadCoins(),
            M4 = slice.LoadCoins(),
            M5 = slice.LoadCoins(),
            M6 = slice.LoadCoins(),
        };
    }
}