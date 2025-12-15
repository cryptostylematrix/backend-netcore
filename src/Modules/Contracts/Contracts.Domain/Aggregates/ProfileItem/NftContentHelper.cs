using System.Text;

// Replace these namespaces/types with your TON C# SDK equivalents.
// The code assumes you have: Cell, Slice, Builder, and Dictionary loading helpers.
namespace Contracts.Domain.Aggregates.ProfileItem;

public static class NftContentHelper
{
    private const byte OffChainContentPrefix = 0x01;

    /// <summary>
    /// Reads a "snake" (linked refs) cell and concatenates all bytes stored in the chain.
    /// </summary>
    public static byte[] FlattenSnakeCell(Cell cell)
    {
        var c = cell;
        var buffers = new List<byte[]>();

        while (c != null)
        {
            var slice = c.Parse();

            // Read current chunk of data
            var bits = slice.RemainderBits;
            if (bits > 0)
            {
                var bytesToRead = bits / 8;
                if (bytesToRead > 0)
                {
                    // SDK mapping: slice.LoadBuffer(bytesToRead) in TS
                    var chunk = slice.LoadBytes(bytesToRead);
                    buffers.Add(chunk);
                }
            }

            // Move to next referenced cell, if any
            c = slice.RemainderRefs > 0 ? 
                slice.LoadRef() : 
                null;
        }

        return Concat(buffers);
    }
    
    private static List<byte[]> BufferToChunks(byte[] buff, int chunkSize)
    {
        var chunks = new List<byte[]>();
        var offset = 0;

        while (offset < buff.Length)
        {
            var len = Math.Min(chunkSize, buff.Length - offset);
            var chunk = new byte[len];
            Buffer.BlockCopy(buff, offset, chunk, 0, len);
            chunks.Add(chunk);
            offset += len;
        }

        return chunks;
    }
    
    
    /// <summary>
    /// Writes data into a snake cell (127 bytes per cell).
    /// </summary>
    public static Cell MakeSnakeCell(byte[] data)
    {
        var chunks = BufferToChunks(data, 127);

        if (chunks.Count == 0)
            return new CellBuilder().Build();

        if (chunks.Count == 1)
            return new CellBuilder().StoreBytes(chunks[0]).Build();

        // Build from tail to head
        var cur = new CellBuilder();
        for (var i = chunks.Count - 1; i >= 0; i--)
        {
            cur.StoreBytes(chunks[i]);

            if (i - 1 >= 0)
            {
                var next = new CellBuilder();
                next.StoreRef(cur.Build());
                cur = next;
            }
        }

        return cur.Build();
    }
    
    
    public static Cell EncodeOffChainContent(string content)
    {
        var data = Encoding.UTF8.GetBytes(content);
        var prefixed = new byte[data.Length + 1];
        prefixed[0] = OffChainContentPrefix;
        Buffer.BlockCopy(data, 0, prefixed, 1, data.Length);

        return MakeSnakeCell(prefixed);
    }

    /// <summary>
    /// Parses dict<uint32, ChunkDictValue> and concatenates values in key order.
    /// </summary>
    public static byte[] ParseChunkDict(CellSlice slice)
    {
        // SDK mapping:
        // TS: slice.loadDict(Dictionary.Keys.Uint(32), ChunkDictValueSerializer)
        var dict = slice.LoadDict(ChunkDictValue.DictLoadingOptions);
            

        // Ensure deterministic concat order (ascending key)
        //var ordered = dict.  ..OrderBy(kv => kv.Key);

        var parts = new List<byte[]>();
        // foreach (var kv in ordered)
        //     parts.Add(kv.Value.Content);

        return Concat(parts);
    }
    
    
    private static byte[] Concat(IEnumerable<byte[]> parts)
    {
        var list = parts as IList<byte[]> ?? parts.ToList();
        var total = list.Sum(p => p.Length);

        var res = new byte[total];
        var pos = 0;
        foreach (var p in list)
        {
            Buffer.BlockCopy(p, 0, res, pos, p.Length);
            pos += p.Length;
        }
        return res;
    }
}
