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
}