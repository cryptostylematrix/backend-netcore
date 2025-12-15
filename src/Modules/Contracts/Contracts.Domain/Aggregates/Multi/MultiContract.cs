namespace Contracts.Domain.Aggregates.Multi;

public sealed class MultiContract
{
    private readonly Address _address;

    private MultiContract(Address address)
    {
        _address = address;
    }

    public static MultiContract CreateFromAddress(Address address)
    {
        return new MultiContract(address);
    }

    public async Task<Result<MinQueueTask>> GetMinQueueTask(ITonClient client, CancellationToken ct = default)
    {
        var result = await client.RunGetMethod(this._address, "get_min_queue_task", Array.Empty<IStackItem>(), null);

        if (result is null)
        {
            return Result<MinQueueTask>.Error(nameof(ContractErrors.GetMethodReturnsNull));
        }

        if (result.Value.ExitCode != 0)
        {
            return Result<MinQueueTask>.Error(nameof(ContractErrors.GetMethodFailed));
        }

        var key = result.Value.Stack.TryGetStruct<BigInteger>(0);
        var val = result.Value.Stack.TryGetClass<Cell>(1);
        
        return Result.Success(new MinQueueTask
        {
            Key = key,
            Val = MultiTaskItem.FromCell(val),
            Flag = (BigInteger)result.Value.Stack[2],
        });
    }
    
    
    public async Task<Result<MultiData>> GetMultiData(ITonClient client, CancellationToken ct = default) {
        var result = await client.RunGetMethod(this._address, "get_multi_data", Array.Empty<IStackItem>(), null);

        if (result is null)
        {
            return Result<MultiData>.Error(nameof(ContractErrors.GetMethodReturnsNull));
        }

        if (result.Value.ExitCode != 0)
        {
            return Result<MultiData>.Error(nameof(ContractErrors.GetMethodFailed));
        }

        var queueCell = result.Value.Stack.TryGetClass<Cell>(7);
        
        var queueSize = (BigInteger)result.Value.Stack[2];
        var seqNo = (BigInteger)result.Value.Stack[3];

        return Result.Success(new MultiData
        {
            Processor = ((Cell)result.Value.Stack[0]).Parse().LoadAddress()!,
            MaxTasks = (BigInteger)result.Value.Stack[1],
            QueueSize = queueSize,
            SeqNo = seqNo,
            Fees = MultiFeesData.FromCell((Cell)result.Value.Stack[4]),
            Security = MultiSecurityData.FromCell((Cell)result.Value.Stack[5]),
            PlaceCode = (Cell)result.Value.Stack[6],
            Tasks = MultiQueueItem.FromCell(queueCell, (uint)queueSize, (uint)seqNo)
        });
    }
}






//
//
// // [ m1:Coins  m2:Coins  m3:Coins  m4:Coins  m5:Coins  m6:Coins ] 
// export type MultiFeesData = {
//     m1: bigint,
//     m2: bigint,
//     m3: bigint,
//     m4: bigint,
//     m5: bigint,
//     m6: bigint,
// };
//
// const getFeeByIndex = (data: MultiFeesData, index: number): bigint => {
//     return Object.values(data)[index] as bigint;
// }
//
// const multiFeesFromCell = (cell: Cell): MultiFeesData => {
//     const slice = cell.beginParse();
//     return {
//         m1: slice.loadCoins(),
//         m2: slice.loadCoins(),
//         m3: slice.loadCoins(),
//         m4: slice.loadCoins(),
//         m5: slice.loadCoins(),
//         m6: slice.loadCoins(),
//     };
// };
//
// const multiFeesToCell = (data: MultiFeesData): Cell => {
//     return beginCell()
//         .storeCoins(data.m1)
//         .storeCoins(data.m2)
//         .storeCoins(data.m3)
//         .storeCoins(data.m4)
//         .storeCoins(data.m5)
//         .storeCoins(data.m6)
//         .endCell();
// };
//
// export const MultiFees = {
//     getFeeByIndex,
//     fromCell: multiFeesFromCell,
//     toCell: multiFeesToCell,
// };
//
// //  [ admin: MsgAddress ]
// export type MultiSecurityData = {
//     admin: Address;
// };
//
// const multiSecurityFromCell = (cell: Cell): MultiSecurityData => {
//     const slice = cell.beginParse();
//     return {
//         admin: slice.loadAddress(),
//     };
// };
//
// export const MultiSecurity = {
//     fromCell: multiSecurityFromCell,
// };
//
// /* _#_ processor: MsgAddress
//     max_tasks: uint16
//     queue_size: uint16
//     seq_no: uint32
//     fees: ^[ m1:Coins  m2:Coins  m3:Coins  m4:Coins  m5:Coins  m6:Coins ]  
//     security: ^[ admin: MsgAddress ]  
//     place_code: ^Cell
//     queue: (HashmapE 32 MultiTask) = MultiStorage; */
//
// export type MultiData = {
//     processor: Address,
//     max_tasks: number,
//     queue_size: number,
//     seq_no: number,
//     fees: MultiFeesData,
//     security: MultiSecurityData,
//     place_code: Cell,
//     queue: Cell | null
// };
//
