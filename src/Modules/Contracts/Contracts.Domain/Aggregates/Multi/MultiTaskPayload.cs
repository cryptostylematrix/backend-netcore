namespace Contracts.Domain.Aggregates.Multi;

public abstract class MultiTaskPayloadBase
{
    public abstract BigInteger Tag { get; }
    public abstract Cell ToCell(CellBuilder builder);
    
    public static MultiTaskPayloadBase FromSlice(CellSlice slice)
    {
        var tag = (uint)slice.LoadUInt(4);

        switch (tag) {
            case 1:
                return new MultiTaskCreatePlacePayload
                {
                    Source = slice.LoadAddress()!,
                    Pos = PlacePosData.FromCell(slice.LoadOptRef())
                };
            case 2:
                return new MultiTaskCreateClonePayload();
            
            case 3:
                return new MultiTaskLockPosPayload
                {
                    Source = slice.LoadAddress()!,
                    Pos = PlacePosData.FromCell(slice.LoadRef())!
                };
            case 4: 
                return new MultiTaskUnlockPosPayload
                {
                    Source = slice.LoadAddress()!,
                    Pos = PlacePosData.FromCell(slice.LoadRef())!
                };
            default:
                throw new NotImplementedException();
        }
    }
}

public sealed class MultiTaskCreatePlacePayload: MultiTaskPayloadBase {
    public override BigInteger Tag => new BigInteger(1);
    
    public Address Source { get; init; } = null!;
    public PlacePosData? Pos { get; init; }
    
    
    public override Cell ToCell(CellBuilder builder)
    {
        return builder.StoreUInt(this.Tag, 4)
            .StoreAddress(this.Source)
            .StoreOptRef(PlacePosData.ToCell(this.Pos))
            .Build();
    }

    
};

public sealed class MultiTaskCreateClonePayload: MultiTaskPayloadBase  {
    public override BigInteger Tag => new BigInteger(2);
    
    public override Cell ToCell(CellBuilder builder)
    {
        return builder.StoreUInt(this.Tag, 4).Build();
    }
};

public sealed class MultiTaskLockPosPayload: MultiTaskPayloadBase {
    public override BigInteger Tag => new BigInteger(3);
    public Address Source { get; init; } = null!;
    public PlacePosData Pos { get; init; } = null!;
    
    public override Cell ToCell(CellBuilder builder)
    {
        return builder.StoreUInt(this.Tag, 4)
            .StoreAddress(this.Source)
            .StoreRef(PlacePosData.ToCell(this.Pos))
            .Build();
    }
};

public sealed class MultiTaskUnlockPosPayload: MultiTaskPayloadBase {
    public override BigInteger Tag => new BigInteger(4);
    public Address Source { get; init; } = null!;
    public PlacePosData Pos { get; init; } = null!;
    
    public override Cell ToCell(CellBuilder builder)
    {
        return builder.StoreUInt(this.Tag, 4)
            .StoreAddress(this.Source)
            .StoreRef(PlacePosData.ToCell(this.Pos))
            .Build();
    }
};