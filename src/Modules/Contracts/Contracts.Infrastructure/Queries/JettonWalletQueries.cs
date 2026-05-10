namespace Contracts.Infrastructure.Queries;


public sealed class JettonWalletQueries(ITonClient tonClient) : IJettonWalletQueries
{
    public async Task<Result<JettonWalletDataResponse>> GetWalletDataAsync(string addr, CancellationToken ct = default)
    {
        try
        {
            var result = await tonClient.RunGetMethod(
                new Address(addr),
                "get_wallet_data",
                Array.Empty<IStackItem>());

            /*
                Stack values:
                    balance: BigInt
                    owner: Address
                    minter: Address
                    wallet_code: Cell
             */

            if (result is null)
                return Result<JettonWalletDataResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<JettonWalletDataResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            var balance = (BigInteger)result.Value.Stack[0];
            var ownerAddr = ((Cell)result.Value.Stack[1]).Parse().LoadAddress()!.ToString();
            var minterAddr = ((Cell)result.Value.Stack[2]).Parse().LoadAddress()!.ToString();
            var walletCode = ((Cell)result.Value.Stack[3]).ToString();


            return Result.Success(new JettonWalletDataResponse
            {
                Balance = (ulong)balance,
                OwnerAddr = ownerAddr,
                MinterAddr = minterAddr,
                WalletCode = walletCode ?? string.Empty
            });
        }
        catch (Exception exc)
        {
            return Result<JettonWalletDataResponse>.Error(exc.Message);
        }
    }
}