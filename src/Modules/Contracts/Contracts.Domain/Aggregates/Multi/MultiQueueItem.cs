namespace Contracts.Domain.Aggregates.Multi;

public sealed class MultiQueueItem
{
    public uint Key { get; init; }
    public MultiTaskItem Val { get; init; } = null!;


    private static HashmapOptions<Bits, MultiTaskItem> DictLoadingOptions => new()
    {
        KeySize = 32,
        Serializers = new HashmapSerializers<Bits, MultiTaskItem>()
        {
            Key = bits => bits, 
            Value = MultiTaskItem.Serialize
        },
        
        Deserializers = new HashmapDeserializers<Bits, MultiTaskItem>()
        {
            Key = bits => bits,
            Value = MultiTaskItem.Deserialize
        }
    };

    static Bits UInt32ToBits(uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        return new Bits(bytes); // exactly 32 bits
    }
    
    public static List<MultiQueueItem> FromCell(Cell? cell, uint queueSize, uint seqNo)
    {
        var result = new List<MultiQueueItem>();
        if (cell is null) return result;
        
        var dict = Hashmap<Bits, MultiTaskItem>.Deserialize(cell, DictLoadingOptions);
        
        var end = seqNo;
        var start = queueSize <= 1 ? 
            seqNo : 
            seqNo - (queueSize - 1);

        for (var keyUInt = start; keyUInt <= end; keyUInt++)
        {
            var keyBits = UInt32ToBits(keyUInt);
            
            var dictValue = dict.Get(keyBits);
            
            if (dictValue is null) continue;

            result.Add(new MultiQueueItem
            {
                Key = keyUInt,
                Val = dictValue
            });
        }

        return result;
    }
}