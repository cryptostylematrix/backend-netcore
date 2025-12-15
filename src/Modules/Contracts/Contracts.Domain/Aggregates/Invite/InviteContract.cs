namespace Contracts.Domain.Aggregates.Invite;

public sealed class InviteContract
{
    private readonly Address _address;

    private InviteContract(Address address)
    {
        _address = address;
    }

    public static InviteContract CreateFromAddress(Address address)
    {
        return new InviteContract(address);
    }
    
    public async Task<Result<InviteData>> GetInviteData(ITonClient client, CancellationToken ct = default) 
    {
        
        var result = await client.RunGetMethod(this._address, "get_invite_data",  Array.Empty<IStackItem>(), null);

        if (result is null)
        {
            return Result<InviteData>.Error(nameof(ContractErrors.GetMethodReturnsNull));
        }
        
        if (result.Value.ExitCode != 0)
        {
            return Result<InviteData>.Error(nameof(ContractErrors.GetMethodFailed));
        }

        var parentCell = result.Value.Stack.TryGetClass<Cell>(4);
        Address? parent = null;
        if (parentCell is not null)
        {
            parent = parentCell.Parse().ReadAddress();
        }
        
        var ownerCell = result.Value.Stack.TryGetClass<Cell>(5);

        return Result.Success(new InviteData
        {
            Admin = ((Cell)result.Value.Stack[0]).Parse().ReadAddress()!,
            Program = (BigInteger)result.Value.Stack[1],
            NextRefNo = (BigInteger)result.Value.Stack[2],
            Number = (BigInteger)result.Value.Stack[3],
            Parent = parent,
            Owner = InviteOwner.FromCell(ownerCell),
        });
    }
    
    public async Task<Result<Address>> GetInviteAddressBySeqNo(ITonClient client, uint seqNo, CancellationToken ct = default)
    {
        var stackItems = new List<IStackItem>()
        {
            new VmStackInt()
            {
                Value = seqNo
            }
        };
        
        var result = await client.RunGetMethod(this._address, "get_invite_address_by_seq_no",  stackItems.ToArray(), null);
        
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