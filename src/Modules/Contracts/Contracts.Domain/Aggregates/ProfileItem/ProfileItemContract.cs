namespace Contracts.Domain.Aggregates.ProfileItem;

public class ProfileItemContract
{
    private readonly Address _address;

    private ProfileItemContract(Address address)
    {
        _address = address;
    }

    public static ProfileItemContract CreateFromAddress(Address address)
    {
        return new ProfileItemContract(address);
    }
    
    public async Task<Result<ProfileData>> GetNftData(ITonClient client, CancellationToken ct = default)
    {
        var result = await client.RunGetMethod(this._address, "get_nft_data",  Array.Empty<IStackItem>(), null);
        
        if (result is null)
        {
            return Result<ProfileData>.Error(nameof(ContractErrors.GetMethodReturnsNull));
        }
        
        if (result.Value.ExitCode != 0)
        {
            return Result<ProfileData>.Error(nameof(ContractErrors.GetMethodFailed));
        }

        var ownerCell = result.Value.Stack.TryGetClass<Cell>(3);
        Address? owner = null;
        if (ownerCell is not null)
        {
            owner = ownerCell.Parse().ReadAddress();
        }
        
        var contentCell = result.Value.Stack.TryGetClass<Cell>(4);

        return Result.Success(new ProfileData
        {
            IsInit = (BigInteger)result.Value.Stack[0],
            Index  = (BigInteger)result.Value.Stack[1],
            Collection =  ((Cell)result.Value.Stack[2]).Parse().ReadAddress()!,
            Owner = owner,
            Content = ProfileContent.FromCell(contentCell) 
        });
    }
    
    public async Task<Result<ProfilePrograms>> GetPrograms(ITonClient client, CancellationToken ct = default)
    {
        var result = await client.RunGetMethod(this._address, "get_programs",  Array.Empty<IStackItem>(), null);
        
        if (result is null)
        {
            return Result<ProfilePrograms>.Error(nameof(ContractErrors.GetMethodReturnsNull));
        }
        
        if (result.Value.ExitCode != 0)
        {
            return Result<ProfilePrograms>.Error(nameof(ContractErrors.GetMethodFailed));
        }
        
        var programsCell = result.Value.Stack.TryGetClass<Cell>(0);
        var programs = ProfilePrograms.FromCell(programsCell);

        return Result.Success(programs);
    }
}