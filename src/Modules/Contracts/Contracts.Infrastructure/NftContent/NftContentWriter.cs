using System.Security.Cryptography;
using Contracts.Infrastructure.Ton;

// using TonSdk.Core.Boc;      // Cell, CellBuilder, CellSlice (typical)
// using TonSdk.Core.Hashmap;  // HashmapE / HashmapOptions (typical)

namespace Contracts.Infrastructure.NftContent;

public static class NftContentWriter
{
    // Same allow-list as TS
    private static readonly HashSet<string> AllowedKeys = new(StringComparer.Ordinal)
    {
        "uri","name","description","image","image_data","symbol","decimals","amount_style",
        "render_type","currency","game","content_type","content_url","lottie","attributes"
    };

    // This is the *writer* equivalent of TS OnChainString().serialize():
    // builder.storeRef(beginCell().storeUint(0,8).storeStringTail(src))
    private static Cell EncodeOnChainString(string value)
    {
        var inner = new CellBuilder()
            .StoreUInt(0, 8)
            .StoreStringTail(value)
            .Build();

        return new CellBuilder()
            .StoreRef(inner)
            .Build();
    }

    /// <summary>
    /// Equivalent of TS nftContentToCell() for onchain-only content.
    /// (If you also need offchain, add the 1-byte tag + StoreStringRefTail(uri) branch.)
    /// </summary>
    public static Cell NftContentToCell(NftContentOnchain content)
    {
        // In TonSdk.NET you typically use HashmapE with options.
        // You already have HashmapOptions<Bits, NftDictValue> on the read side.
        // For writing, we store Bits -> Cell (value is a Cell that contains the ref wrapper).
        var dictOptions = new HashmapOptions<Bits, Cell>
        {
            KeySize = 256,
            Serializers = new HashmapSerializers<Bits, Cell>
            {
                Key = b => b,
                Value = c => c
            },
            Deserializers = new HashmapDeserializers<Bits, Cell>
            {
                Key = b => b,
                Value = c => c
            }
        };

        var dict = new HashmapE<Bits, Cell>(dictOptions);

        foreach (var (key, val) in content.Data)
        {
            if (!AllowedKeys.Contains(key)) continue;
            if (string.IsNullOrEmpty(val)) continue;

            var keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key)); // 32 bytes
            var bitsKey = new Bits(keyHash);                            // 256-bit key

            dict.Set(bitsKey, EncodeOnChainString(val));
        }

        // Outer onchain content cell: 0x00 + dict
        return new CellBuilder()
            .StoreUInt(0, 8)
            .StoreDict(dict)
            .Build();
    }

    // Optional: offchain branch (TS content.type == 'offchain')
    public static Cell OffchainToCell(string uri)
        => new CellBuilder()
            .StoreUInt(1, 8)
            .StoreStringTail(uri) // snake under the hood
            .Build();
}