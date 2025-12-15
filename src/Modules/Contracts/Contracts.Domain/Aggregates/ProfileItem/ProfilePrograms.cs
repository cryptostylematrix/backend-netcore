namespace Contracts.Domain.Aggregates.ProfileItem;

public sealed class ProfilePrograms
{
    public ProgramData? Multi { get; private set; }
    
    public static HashmapOptions<Bits, ProgramData> DictLoadingOptions => new()
    {
        KeySize = 32,
        Serializers = new HashmapSerializers<Bits, ProgramData>()
        {
            Key = bits => bits, 
            Value = ProgramData.Serialize
        },
        
        Deserializers = new HashmapDeserializers<Bits, ProgramData>()
        {
            Key = bits => bits,
            Value = ProgramData.Deserialize
        }
    };
    
    public static ProfilePrograms FromCell(Cell? content)
    {
        var result = new ProfilePrograms();

        if (content is null) return result;
        
        try
        {
            var dict = Hashmap<Bits, ProgramData>.Deserialize(content, DictLoadingOptions);
            
            uint multiHash = 0x1ce8c484;

            // convert to big-endian bytes
            var bytes = BitConverter.GetBytes(multiHash);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            // create 32-bit Bits
            var multiKey = new Bits(bytes);
            
            
            // get multi
            var dictValue = dict.Get(multiKey);
            if (dictValue is not null)
            {
                result.Multi = dictValue;
            }
            
            return result;
        }
        catch
        {
            // Replace with your logger if needed
            return result;
        }
    }
}