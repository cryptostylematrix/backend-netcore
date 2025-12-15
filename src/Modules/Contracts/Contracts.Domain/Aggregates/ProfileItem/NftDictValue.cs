namespace Contracts.Domain.Aggregates.ProfileItem;

public sealed class NftDictValue
{
    public required byte[] Content { get; init; }
    
    public static HashmapOptions<Bits, NftDictValue> DictLoadingOptions => new()
    {
        KeySize = 256,
        Serializers = new HashmapSerializers<Bits, NftDictValue>()
        {
            Key = bits => bits, 
            Value = val => new CellBuilder().Build()
        },
        Deserializers = new HashmapDeserializers<Bits, NftDictValue>()
        {
            Key = bits => bits,
            Value = cell =>
            {
                // TS:
                // const ref = src.loadRef().asSlice();
                // const start = ref.loadUint(8);
                var innerSlice = cell.Parse().LoadRef().Parse();

                var start = (byte)innerSlice.LoadUInt(8);

                switch (start)
                {
                    case 0:
                    {
                        // TS intent: flatten snake from the remaining slice/cell
                        // IMPORTANT: after reading 8 bits, we want the remaining payload.
                        // Many SDKs provide something like innerSlice.ToCell() or innerSlice.AsCell().
                        var remainingCell = new CellBuilder().StoreCellSlice(innerSlice).Build();
                        var snake = NftContentHelper.FlattenSnakeCell(remainingCell);
                        return new NftDictValue { Content = snake };
                    }
                    case 1:
                    {
                        // dict of chunks starts immediately after the "1" byte
                        var buf = NftContentHelper.ParseChunkDict(innerSlice);
                        return new NftDictValue { Content = buf };
                    }
                    default:
                        return new NftDictValue { Content = [] };
                }
            }
        }
    };
}