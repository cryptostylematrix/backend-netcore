namespace Contracts.Domain.Aggregates.Place;

public sealed class PlaceContract
{
    private readonly Address _address;

    private PlaceContract(Address address)
    {
        _address = address;
    }

    public static PlaceContract CreateFromAddress(Address address)
    {
        return new PlaceContract(address);
    }


    public async Task<Result<PlaceData>> GetPlaceData(ITonClient client, CancellationToken ct = default)
    {
        var result = await client.RunGetMethod(this._address, "get_place_data", Array.Empty<IStackItem>(), null);

        if (result is null)
        {
            return Result<PlaceData>.Error(nameof(ContractErrors.GetMethodReturnsNull));
        }

        if (result.Value.ExitCode != 0)
        {
            return Result<PlaceData>.Error(nameof(ContractErrors.GetMethodFailed));
        }

        var parentCell = result.Value.Stack.TryGetClass<Cell>(2);
        Address? parent = null;
        if (parentCell is not null)
        {
            parent = parentCell.Parse().ReadAddress();
        }

        var childrenCell = result.Value.Stack.TryGetClass<Cell>(7);
        
        return Result.Success(new PlaceData
        {
            Marketing = ((Cell)result.Value.Stack[0]).Parse().ReadAddress()!,
            M = (BigInteger)result.Value.Stack[1],
            Parent = parent,
            CreatedAt = (BigInteger)result.Value.Stack[3],
            FillCount = (BigInteger)result.Value.Stack[4],
            Profiles = PlaceProfiles.FromCell((Cell)result.Value.Stack[5]),
            Security = PlaceSecurity.FromCell((Cell)result.Value.Stack[6]),
            Children = PlaceChildren.FromCell(childrenCell)
        });
    }
}