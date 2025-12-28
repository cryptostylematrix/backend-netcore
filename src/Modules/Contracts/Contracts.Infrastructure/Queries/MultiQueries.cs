namespace Contracts.Infrastructure.Queries;

public sealed class MultiQueries(ITonClient tonClient, IOptions<TonContractAddressesOptions> opts)
    : IMultiQueries
{
    private readonly Address _multiAddress = new(opts.Value.MultiAddr);

    public async Task<Result<MinQueueTaskResponse>> GetMinQueueTaskAsync(CancellationToken ct = default)
    {
        try
        {
            // exactly what MultiContract did, but build DTOs
            var result = await tonClient.RunGetMethod(
                _multiAddress,
                "get_min_queue_task",
                Array.Empty<IStackItem>());

            if (result is null)
                return Result<MinQueueTaskResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<MinQueueTaskResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            var key = result.Value.Stack.TryGetStruct<BigInteger>(0);
            var valCell = result.Value.Stack.TryGetClass<Cell>(1);
            var flag = (BigInteger)result.Value.Stack[2];

            return Result.Success(new MinQueueTaskResponse
            {
                Key = key is null ? null : (int)key.Value,
                Val = MultiTaskItemFromCell(valCell),
                Flag = (int)flag
            });
        }
        catch (Exception exc)
        {
            return Result<MinQueueTaskResponse>.Error(exc.Message);
        }
    }

    public async Task<Result<MultiDataResponse>> GetMultiDataAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await tonClient.RunGetMethod(
                _multiAddress,
                "get_multi_data",
                Array.Empty<IStackItem>());

            if (result is null)
                return Result<MultiDataResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<MultiDataResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            // Stack layout (from your MultiContract):
            // 0 processor (cell->address)
            // 1 max_tasks
            // 2 queue_size
            // 3 seq_no
            // 4 fees cell
            // 5 security cell
            // 6 place_code cell
            // 7 queue dict cell (optional)
            var processor = ((Cell)result.Value.Stack[0]).Parse().LoadAddress()!.ToString();

            var maxTasks = (BigInteger)result.Value.Stack[1];
            var queueSize = (BigInteger)result.Value.Stack[2];
            var seqNo = (BigInteger)result.Value.Stack[3];

            var feesCell = (Cell)result.Value.Stack[4];
            var secCell = (Cell)result.Value.Stack[5];
            var placeCodeCell = (Cell)result.Value.Stack[6];
            var queueCell = result.Value.Stack.TryGetClass<Cell>(7);

            var fees = MultiFeesFromCell(feesCell);
            var security = MultiSecurityFromCell(secCell);

            // If you want something else (base64/boc), adjust here to what your API expects.
            // I'm using ToString() as a placeholder because you had string in DTO but domain used Cell.
            var placeCode = placeCodeCell.ToString();

            var tasks = MultiQueueItemsFromCell(queueCell, (uint)queueSize, (uint)seqNo)
                .ToArray();

            return Result.Success(new MultiDataResponse
            {
                ProcessorAddr = processor,
                MaxTasks = (uint)maxTasks,
                QueueSize = (uint)queueSize,
                SeqNo = (uint)seqNo,
                Fees = fees,
                Security = security,
                PlaceCode = placeCode,
                Tasks = tasks
            });
        }
        catch (Exception exc)
        {
            return Result<MultiDataResponse>.Error(exc.Message);
        }
    }

    // -----------------------------
    // DTO parsers
    // -----------------------------

    private static MultiFeesDataResponse MultiFeesFromCell(Cell cell)
    {
        var slice = cell.Parse();
        var m1 = slice.LoadCoins();
        var m2 = slice.LoadCoins();
        var m3 = slice.LoadCoins();
        var m4 = slice.LoadCoins();
        var m5 = slice.LoadCoins();
        var m6 = slice.LoadCoins();

        // Your mapping used Coins.ToDecimal()
        return new MultiFeesDataResponse
        {
            M1 = m1.ToDecimal(),
            M2 = m2.ToDecimal(),
            M3 = m3.ToDecimal(),
            M4 = m4.ToDecimal(),
            M5 = m5.ToDecimal(),
            M6 = m6.ToDecimal()
        };
    }

    private static MultiSecurityDataResponse MultiSecurityFromCell(Cell cell)
    {
        var slice = cell.Parse();
        var admin = slice.LoadAddress()!.ToString();

        return new MultiSecurityDataResponse
        {
            AdminAddr = admin
        };
    }

    private static MultiTaskItemResponse? MultiTaskItemFromCell(Cell? cell)
    {
        if (cell is null) return null;

        var slice = cell.Parse();

        var queryId = (long)slice.LoadUInt(64);
        var m = (short)slice.LoadUInt(3);
        var profileAddr = slice.LoadAddress()!.ToString();

        var payload = MultiTaskPayloadFromSlice(slice);

        return new MultiTaskItemResponse
        {
            QueryId = queryId,
            M = m,
            ProfileAddr = profileAddr,
            Payload = payload
        };
    }

    private static MultiTaskPayloadResponse MultiTaskPayloadFromSlice(CellSlice slice)
    {
        var tag = (short)slice.LoadUInt(4);

        return tag switch
        {
            1 => new MultiTaskPayloadResponse
            {
                Tag = tag,
                SourceAddr = slice.LoadAddress()!.ToString(),
                Pos = PlacePosFromCell(slice.LoadOptRef())
            },
            2 => new MultiTaskPayloadResponse
            {
                Tag = tag,
                SourceAddr = null,
                Pos = null
            },
            3 => new MultiTaskPayloadResponse
            {
                Tag = tag,
                SourceAddr = slice.LoadAddress()!.ToString(),
                Pos = PlacePosFromCell(slice.LoadRef())
            },
            4 => new MultiTaskPayloadResponse
            {
                Tag = tag,
                SourceAddr = slice.LoadAddress()!.ToString(),
                Pos = PlacePosFromCell(slice.LoadRef())
            },
            _ => throw new NotImplementedException($"Unknown MultiTask payload tag: {tag}")
        };
    }

    private static PlacePosDataResponse? PlacePosFromCell(Cell? cell)
    {
        if (cell is null) return null;

        var s = cell.Parse();
        return new PlacePosDataResponse
        {
            ParentAddr = s.LoadAddress()!.ToString(),
            Pos = (short)s.LoadUInt(1)
        };
    }

    private static IEnumerable<MultiQueueItemResponse> MultiQueueItemsFromCell(Cell? cell, uint queueSize, uint seqNo)
    {
        if (cell is null) yield break;

        var dict = Hashmap<Bits, Cell>.Deserialize(cell, MultiQueueDictOptions);

        var end = seqNo;
        var start = queueSize <= 1 ? seqNo : seqNo - (queueSize - 1);

        for (var keyUInt = start; keyUInt <= end; keyUInt++)
        {
            var keyBits = UInt32ToBits(keyUInt);

            var taskCell = dict.Get(keyBits);
            if (taskCell is null) continue;

            var val = MultiTaskItemFromCell(taskCell);
            if (val is null) continue;

            yield return new MultiQueueItemResponse
            {
                Key = keyUInt,
                Val = val
            };
        }
    }

    private static HashmapOptions<Bits, Cell> MultiQueueDictOptions => new()
    {
        KeySize = 32,
        Serializers = new HashmapSerializers<Bits, Cell>
        {
            Key = bits => bits,
            Value = _ => new CellBuilder().Build()
        },
        Deserializers = new HashmapDeserializers<Bits, Cell>
        {
            Key = bits => bits,
            Value = c => c
        }
    };

    private static Bits UInt32ToBits(uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return new Bits(bytes);
    }
}
