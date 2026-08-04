namespace Contracts.Infrastructure.Queries;


public sealed class JettonWalletQueries(ITonClient tonClient) : IJettonWalletQueries
{
    private const uint TransferTag = 0x0f8a7ea5;

    public Result<JettonTransferMsgBodyResponse> BuildTransferMsgBody(
        ulong queryId,
        ulong amount,
        string destinationAddr,
        string? responseDestinationAddr,
        string? customPayloadBocHex,
        ulong forwardTonAmount,
        string? forwardPayloadBocHex)
    {
        try
        {
            var builder = new CellBuilder();
            builder.StoreUInt(TransferTag, 32);
            builder.StoreUInt(queryId, 64);
            builder.StoreCoins(new Coins(amount, new CoinsOptions(IsNano: true)));
            builder.StoreAddress(new Address(destinationAddr));
            builder.StoreAddress(ParseAddress(responseDestinationAddr));
            builder.StoreOptRef(ParseCell(customPayloadBocHex));
            builder.StoreCoins(new Coins(forwardTonAmount, new CoinsOptions(IsNano: true)));
            builder.StoreOptRef(ParseCell(forwardPayloadBocHex));

            return Result.Success(new JettonTransferMsgBodyResponse
            {
                BocHex = builder.Build().ToString("hex").ToLowerInvariant()
            });
        }
        catch (Exception exc)
        {
            return Result<JettonTransferMsgBodyResponse>.Error(exc.Message);
        }
    }

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

    private static Address? ParseAddress(string? address) =>
        string.IsNullOrWhiteSpace(address) ? null : new Address(address);

    private static Cell? ParseCell(string? bocHex) =>
        string.IsNullOrWhiteSpace(bocHex)
            ? null
            : Cell.From(new Bits(Convert.FromHexString(bocHex)));
}
