using System.Security.Cryptography;

namespace Contracts.Infrastructure.Queries;


public sealed class JetttonMinterQueries(ITonClient tonClient) : IJetttonMinterQueries
{
    public async Task<Result<JettonWalletAddressResponse>> GetWalletAddressAsync(string addr, string ownerAddr, CancellationToken ct = default)
    {
        try
        {
            var stackItems = new IStackItem[]
            {
                new VmStackSlice() { Value = new CellBuilder().StoreAddress(new Address(ownerAddr)).Build().Parse() }
            };
            
            var result = await tonClient.RunGetMethod(
                new Address(addr),
                "get_wallet_address",
                stackItems);

            if (result is null)
                return Result<JettonWalletAddressResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<JettonWalletAddressResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            var walletAddr = ((Cell)result.Value.Stack[0]).Parse().LoadAddress()!.ToString();

            return Result.Success(new JettonWalletAddressResponse
            {
                WalletAddr= walletAddr.ToString()
            });
        }
        catch (Exception exc)
        {
            return Result<JettonWalletAddressResponse>.Error(exc.Message);
        }
    }

    public async Task<Result<JettonMinterDataResponse>> GetJettonDataAsync(
        string addr,
        CancellationToken ct = default)
    {
        try
        {
            var result = await tonClient.RunGetMethod(
                new Address(addr),
                "get_jetton_data",
                Array.Empty<IStackItem>());

            if (result is null)
                return Result<JettonMinterDataResponse>.Error(
                    nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<JettonMinterDataResponse>.Error(
                    nameof(ContractErrors.GetMethodFailed));

            var stack = result.Value.Stack;
            var content = stack.TryGetClass<Cell>(3)
                ?? throw new InvalidOperationException("Jetton content was not returned.");
            var walletCode = stack.TryGetClass<Cell>(4)
                ?? throw new InvalidOperationException("Jetton wallet code was not returned.");
            var adminAddress = ((Cell)stack[2]).Parse().LoadAddress()?.ToString()
                ?? throw new InvalidOperationException("Jetton admin address was not returned.");
            var metadata = ParseMetadata(content);

            return Result.Success(new JettonMinterDataResponse
            {
                TotalSupply = ((BigInteger)stack[0]).ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                Mintable = (BigInteger)stack[1] != BigInteger.Zero,
                AdminAddress = adminAddress,
                MetadataUri = metadata.Uri,
                Decimals = metadata.Decimals,
                ContentBocHex = content.ToString("hex").ToLowerInvariant(),
                WalletCodeBocHex = walletCode.ToString("hex").ToLowerInvariant()
            });
        }
        catch (Exception exc)
        {
            return Result<JettonMinterDataResponse>.Error(exc.Message);
        }
    }

    private static JettonMetadata ParseMetadata(Cell content)
    {
        var slice = content.Parse();
        var contentPrefix = checked((byte)slice.LoadUInt(8));

        if (contentPrefix != 0)
            throw new InvalidOperationException("Jetton content is not in on-chain metadata format.");

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

        return new JettonMetadata(uri, decimals);
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

                if (valuePrefix != 0)
                    return new MetadataValue(null);

                var valueCell = new CellBuilder()
                    .StoreCellSlice(valueSlice)
                    .Build();

                return new MetadataValue(
                    Encoding.UTF8.GetString(FlattenSnakeCell(valueCell)));
            }
        }
    };

    private static byte[] FlattenSnakeCell(Cell cell)
    {
        var parts = new List<byte[]>();
        Cell? current = cell;

        while (current is not null)
        {
            var slice = current.Parse();
            var bytesToRead = slice.RemainderBits / 8;

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

    private sealed record JettonMetadata(string? Uri, byte? Decimals);
}
