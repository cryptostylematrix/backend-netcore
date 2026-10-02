using Contracts.Infrastructure.Metadata;

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
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
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
            var adminAddress = ((Cell)stack[2]).Parse().LoadAddress()?.ToString();
            var metadata = JettonContentMetadata.Parse(content);

            return Result.Success(new JettonMinterDataResponse
            {
                TotalSupply = ((BigInteger)stack[0]).ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                Mintable = (BigInteger)stack[1] != BigInteger.Zero,
                AdminAddress = adminAddress,
                MetadataUri = metadata.Uri,
                Decimals = metadata.Decimals,
                Name = metadata.Name,
                Symbol = metadata.Symbol,
                ContentBocHex = content.ToString("hex").ToLowerInvariant(),
                WalletCodeBocHex = walletCode.ToString("hex").ToLowerInvariant()
            });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception exc)
        {
            return Result<JettonMinterDataResponse>.Error(exc.Message);
        }
    }

}
