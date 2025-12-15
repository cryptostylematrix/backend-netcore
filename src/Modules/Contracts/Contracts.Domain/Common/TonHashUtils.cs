using System.Security.Cryptography;
using System.Text;

public static class TonHashUtils
{
    // JS: BigInt("0x" + sha256(textUtf8).toString("hex"))
    public static BigInteger Sha256n(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);

        var hash = SHA256.HashData(bytes); // 32 bytes

        // BigInteger expects little-endian two's-complement.
        // Convert from big-endian hash to a positive BigInteger:
        var littleEndianUnsigned = hash.Reverse()
            .Concat(new byte[] { 0x00 }) // force positive
            .ToArray();

        return new BigInteger(littleEndianUnsigned);
    }
}