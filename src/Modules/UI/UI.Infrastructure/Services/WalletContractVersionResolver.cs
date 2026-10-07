using TonSdk.Core;
using TonSdk.Core.Boc;
using UI.Application.Abstractions;

namespace UI.Infrastructure.Services;

internal sealed class WalletContractVersionResolver : IWalletContractVersionResolver
{
    // Standard wallet code hashes: https://docs.ton.org/contracts/standard/wallets/history
    private static readonly IReadOnlyDictionary<string, string> Versions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["a0cfc2c48aee16a271f2cfc0b7382d81756cecb1017d077faaab3bb602f6868c"] = "v1r1",
            ["d4902fcc9fad74698fa8e353220a68da0dcf72e32bcb2eb9ee04217c17d3062c"] = "v1r2",
            ["587cc789eff1c84f46ec3797e45fc809a14ff5ae24f1e0c7a6a99cc9dc9061ff"] = "v1r3",
            ["5c9a5e68c108e18721a07c42f9956bfb39ad77ec6d624b60c576ec88eee65329"] = "v2r1",
            ["fe9530d3243853083ef2ef0b4c2908c0abf6fa1c31ea243aacaa5bf8c7d753f1"] = "v2r2",
            ["b61041a58a7980b946e8fb9e198e3c904d24799ffa36574ea4251c41a566f581"] = "v3r1",
            ["84dafa449f98a6987789ba232358072bc0f76dc4524002a5d0918b9a75d2d599"] = "v3r2",
            ["64dd54805522c5be8a9db59cea0105ccf0d08786ca79beb8cb79e880a8d7322d"] = "v4r1",
            ["feb5ff6820e2ff0d9483e7e0d62c817d846789fb4ae580c878866d959dabd5c0"] = "v4r2",
            ["20834b7b72b112147e1b2fb457b84e74d1a30f04f737d4f62a668e9552d2b72f"] = "v5r1"
        };

    public bool TryResolve(string walletAddr, string? stateInit, out string version)
    {
        version = "unknown version";
        if (string.IsNullOrWhiteSpace(stateInit)) return true;
        if (stateInit.Length > 65536) return false;

        try
        {
            var roots = BagOfCells.DeserializeBoc(new Bits(Convert.FromBase64String(stateInit)));
            if (roots.Length != 1 || roots[0].IsExotic) return false;
            var root = roots[0];
            if (!root.Hash.ToBytes().SequenceEqual(new Address(walletAddr).GetHash()))
                return false;

            // StateInit TL-B: optional split depth, special, code, data, library.
            var slice = root.Parse();
            if (slice.LoadBit()) slice.SkipBits(5);
            if (slice.LoadBit()) slice.SkipBits(2);
            var code = slice.LoadOptRef();
            slice.LoadOptRef();
            slice.LoadOptRef();
            if (slice.RemainderBits != 0 || slice.RemainderRefs != 0) return false;
            if (code is not null && !code.IsExotic)
                version = Versions.GetValueOrDefault(Convert.ToHexString(code.Hash.ToBytes()), version);
            return true;
        }
        catch (Exception error) when (error.GetType() == typeof(Exception)
            || error is ArgumentException or FormatException or InvalidOperationException
                or IndexOutOfRangeException or OverflowException)
        {
            // The vendored BOC parser also throws plain Exception for malformed input.
            return false;
        }
    }
}
