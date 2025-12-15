namespace Contracts.Domain.Aggregates.ProfileItem;

public sealed class ChunkDictValue
{
    public required byte[] Content { get; init; }
    
    public static HashmapOptions<Bits, ChunkDictValue> DictLoadingOptions => new()
    {
        KeySize = 256,
        Serializers = new HashmapSerializers<Bits, ChunkDictValue>()
        {
            Key = bits => bits,
            Value = val => new CellBuilder().Build()
        },
        Deserializers = new HashmapDeserializers<Bits, ChunkDictValue>()
        {
            Key = bits => bits,
            Value = cell =>
            {
                var snakeCell = cell.Parse().LoadRef();
                var snake = NftContentHelper.FlattenSnakeCell(snakeCell);
                return new ChunkDictValue { Content = snake };
            }
        }
    };
}