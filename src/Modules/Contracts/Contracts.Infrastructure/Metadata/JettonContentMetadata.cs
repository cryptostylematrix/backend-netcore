using System.Security.Cryptography;

namespace Contracts.Infrastructure.Metadata;

internal sealed record JettonContentMetadata(string? Uri, string? Name, string? Symbol, byte? Decimals)
{
    public static JettonContentMetadata Parse(Cell content)
    {
        var slice = content.Parse();
        var contentPrefix = checked((byte)slice.LoadUInt(8));

        if (contentPrefix == 1)
            return new(Encoding.UTF8.GetString(FlattenSnakeCell(new CellBuilder().StoreCellSlice(slice).Build())), null, null, null);
        if (contentPrefix != 0)
            throw new InvalidOperationException("Unsupported Jetton metadata format.");

        var dictionary = slice.LoadDict(MetadataDictionaryOptions);
        var uri = GetMetadataValue(dictionary, "uri");
        var decimalsText = GetMetadataValue(dictionary, "decimals");

        byte? decimals = null;
        if (!string.IsNullOrWhiteSpace(decimalsText))
        {
            if (!byte.TryParse(
                    decimalsText,
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsedDecimals))
            {
                throw new InvalidOperationException("Jetton metadata decimals value is invalid.");
            }

            decimals = parsedDecimals;
        }

        return new(uri, GetMetadataValue(dictionary, "name"), GetMetadataValue(dictionary, "symbol"), decimals);
    }

    private static string? GetMetadataValue(
        HashmapE<Bits, MetadataValue> dictionary,
        string key)
    {
        var keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return dictionary.Get(new Bits(keyHash))?.Value;
    }

    private static HashmapOptions<Bits, MetadataValue> MetadataDictionaryOptions => new()
    {
        KeySize = 256,
        Serializers = new HashmapSerializers<Bits, MetadataValue>
        {
            Key = bits => bits,
            Value = _ => new CellBuilder().Build()
        },
        Deserializers = new HashmapDeserializers<Bits, MetadataValue>
        {
            Key = bits => bits,
            Value = cell =>
            {
                var valueSlice = cell.Parse().LoadRef().Parse();
                var valuePrefix = checked((byte)valueSlice.LoadUInt(8));

                if (valuePrefix == 0)
                    return new MetadataValue(Encoding.UTF8.GetString(FlattenSnakeCell(
                        new CellBuilder().StoreCellSlice(valueSlice).Build())));
                if (valuePrefix == 1)
                {
                    var chunks = valueSlice.LoadDict(new HashmapOptions<uint, Cell>
                    {
                        KeySize = 32,
                        Serializers = new() { Key = key => new BitsBuilder(32).StoreUInt(key, 32).Build(), Value = cell => new CellBuilder().StoreRef(cell).Build() },
                        Deserializers = new() { Key = bits => (uint)bits.Parse().LoadUInt(32), Value = cell => cell.Parse().LoadRef() }
                    });
                    using var bytes = new MemoryStream();
                    for (uint i = 0; i < chunks.Count; i++)
                    {
                        var chunk = chunks.Get(i) ?? throw new InvalidOperationException("Missing metadata chunk.");
                        bytes.Write(FlattenSnakeCell(chunk));
                        if (bytes.Length > 262144) throw new InvalidOperationException("Metadata is too large.");
                    }
                    return new MetadataValue(Encoding.UTF8.GetString(bytes.ToArray()));
                }
                throw new InvalidOperationException("Unsupported metadata value format.");
            }
        }
    };

    private static byte[] FlattenSnakeCell(Cell cell)
    {
        var parts = new List<byte[]>();
        Cell? current = cell;
        var length = 0;

        while (current is not null)
        {
            var slice = current.Parse();
            if (slice.RemainderBits % 8 != 0 || slice.RemainderRefs > 1)
                throw new InvalidOperationException("Invalid metadata snake.");
            var bytesToRead = slice.RemainderBits / 8;
            length += bytesToRead;
            if (length > 262144) throw new InvalidOperationException("Metadata is too large.");

            if (bytesToRead > 0)
                parts.Add(slice.LoadBytes(bytesToRead));

            current = slice.RemainderRefs > 0 ? slice.LoadRef() : null;
        }

        var result = new byte[parts.Sum(part => part.Length)];
        var offset = 0;

        foreach (var part in parts)
        {
            Buffer.BlockCopy(part, 0, result, offset, part.Length);
            offset += part.Length;
        }

        return result;
    }

    private sealed record MetadataValue(string? Value);

}
