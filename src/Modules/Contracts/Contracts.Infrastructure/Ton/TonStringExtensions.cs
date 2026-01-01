namespace Contracts.Infrastructure.Ton;

public static class TonStringExtensions
{
    /// <summary>
    /// Equivalent to @ton/core builder.storeStringTail(src)
    /// Writes UTF-8 bytes into current cell; if it doesn't fit, spills remainder into a ref cell (snake).
    /// </summary>
    public static CellBuilder StoreStringTail(this CellBuilder builder, string src)
    {
        if (src == null) throw new ArgumentNullException(nameof(src));
        var bytes = Encoding.UTF8.GetBytes(src);
        WriteBufferSnake(bytes, builder);
        return builder;
    }

    /// <summary>
    /// Equivalent to @ton/core builder.storeStringRefTail(src)
    /// i.e., storeRef(beginCell().storeStringTail(src).endCell())
    /// </summary>
    public static CellBuilder StoreStringRefTail(this CellBuilder builder, string src)
    {
        if (src == null) throw new ArgumentNullException(nameof(src));
        var cell = new CellBuilder().StoreStringTail(src).Build();
        return builder.StoreRef(cell);
    }

    private static void WriteBufferSnake(ReadOnlySpan<byte> src, CellBuilder builder)
    {
        if (src.Length == 0) return;

        // @ton/core: bytes = floor(builder.availableBits / 8)
        var bytesThatFit = builder.RemainderBits / 8;

        // Important edge case:
        // If there are remaining bytes but we can't fit even 1 byte in this cell,
        // we must move everything to a ref cell to avoid infinite recursion.
        if (bytesThatFit <= 0)
        {
            var next = new CellBuilder();
            WriteBufferSnake(src, next);
            builder.StoreRef(next.Build());
            return;
        }

        if (src.Length > bytesThatFit)
        {
            // store first chunk here
            builder.StoreBytes(src.Slice(0, bytesThatFit).ToArray());

            // remainder goes into a new ref cell (recursively)
            var next = new CellBuilder();
            WriteBufferSnake(src.Slice(bytesThatFit), next);
            builder.StoreRef(next.Build());
        }
        else
        {
            // all fits in this cell
            builder.StoreBytes(src.ToArray());
        }
    }
}