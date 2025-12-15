namespace Contracts.Domain.Aggregates.ProfileCollection;

public sealed class ProfileCollectionContract
{
    private readonly Address _address;

    private ProfileCollectionContract(Address address)
    {
        _address = address;
    }

    public static ProfileCollectionContract CreateFromAddress(Address address)
    {
        return new ProfileCollectionContract(address);
    }
    
    public async Task<Result<Address>> GetNftAddressByIndex(ITonClient client, BigInteger index, CancellationToken ct = default)
    {
        var stackItems = new List<IStackItem>()
        {
            new VmStackInt()
            {
                Value = index
            }
        };
        
        var result = await client.RunGetMethod(this._address, "get_nft_address_by_index",  stackItems.ToArray(), null);
        
        if (result is null)
        {
            return Result<Address>.Error(nameof(ContractErrors.GetMethodReturnsNull));
        }
        
        if (result.Value.ExitCode != 0)
        {
            return Result<Address>.Error(nameof(ContractErrors.GetMethodFailed));
        }
        
        return Result.Success(((Cell)result.Value.Stack[0]).Parse().ReadAddress()!);
    }
}