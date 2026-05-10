namespace Contracts.Infrastructure.Queries;

public sealed class MarketingQueries(ITonClient tonClient) : IMarketingQueries
{
    public async Task<Result<MarketingTransactionHistoryResponse>> GetMarketingHistoryAsync(
        string addr, 
        uint limit,
        ulong? lt,
        string? hash,
        CancellationToken ct = default)
    {
        try
        {
            var result = await tonClient.GetTransactions(
                address: new Address(addr),
                limit: limit,
                lt: lt,
                hash: hash,
                to_lt: null,
                archival: true);

            if (result is null)
                return Result<MarketingTransactionHistoryResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));
            
            var response = new MarketingTransactionHistoryResponse
            {
                Items = result.Select(tr => new MarketingTransactionResponse
                {
                    Lt = tr.TransactionId.Lt,
                    Hash = tr.TransactionId.Hash,
                    UTime = tr.UTime,
                    Messages = MarkeetingTransactionMessageFactory.Create(tr)
                }).ToArray(),
            };
        
            return Result.Success(response);
        }
        catch (Exception exc)
        {
            return Result<MarketingTransactionHistoryResponse>.Error(exc.Message);
        }
    }
    
    // _#_ parent:MsgAddress pos:uint32 = PlacePos;
    private static Cell? PosToCell(string? parentAddr, int? pos)
    {
        if (parentAddr is null) return null;
        if (pos is null) return null;

        var notNullPos = (int)pos;
        
        var builder = new CellBuilder();
        builder.StoreAddress(new Address(parentAddr));
        builder.StoreUInt(notNullPos, 32);
        return builder.Build();
    }
    
    private static PosDataResponse? PosFromCell(Cell? cell)
    {
        if (cell is null) return null;

        var s = cell.Parse();
        return new PosDataResponse
        {
            ParentAddr = s.LoadAddress()!.ToString(),
            Pos = (uint)s.LoadUInt(32)
        };
    }
    
    // buy_place#be490d70  query_id:uint64  m:uint8  profile:Address  first:Bool  pos:(Maybe ^PlacePos) = MarketingInternalMsg;
    public Result<BuyPlaceByTonBodyResponse> BuildBuyPlaceByTonBody(long queryId, int m, string profileAddr, 
        bool first, string? parentAddr, int? pos)
    {
        try
        {
            var builder = new CellBuilder();
            builder.StoreUInt(0xbe490d70, 32); // buy_place
            builder.StoreUInt(queryId, 64);
            builder.StoreUInt(m, 8);
            builder.StoreAddress(new Address(profileAddr));
            builder.StoreBit(first);
            builder.StoreOptRef(PosToCell(parentAddr, pos));
            
            return Result<BuyPlaceByTonBodyResponse>.Success(new BuyPlaceByTonBodyResponse
            {
                BocHex = builder.Build().ToString("hex").ToLower()
            });
        }
        catch (Exception e)
        {
            return Result<BuyPlaceByTonBodyResponse>.Error(e.Message);
        }
    }

    /*
    transfer#0f8a7ea5
       query_id:uint64
       amount:Coins
       destination:MsgAddress
       response_destination:MsgAddress
       custom_payload:(Maybe ^Cell)
       forward_ton_amount:Coins
       forward_payload:(Either Cell ^Cell) = JettonMsg;
       
    buy_place#be490d70  query_id:uint64  m:uint8  profile:Address  first:Bool  pos:(Maybe ^PlacePos) = MarketingInternalMsg;
     */
    public Result<BuyPlaceByJettonBodyResponse> BuildBuyPlaceByJettonBody(long queryId, string marketingAddr, int m, string profileAddr, bool first,
        string? parentAddr, int? pos, ulong amount, string senderAddr, ulong fee)
    {
        try
        {
            // forward payload
            var fpBuilder = new CellBuilder();
            fpBuilder.StoreUInt(0xbe490d70, 32); // buy_place
            fpBuilder.StoreUInt(queryId, 64);
            fpBuilder.StoreUInt(m, 8);
            fpBuilder.StoreAddress(new Address(profileAddr));
            fpBuilder.StoreBit(first);
            fpBuilder.StoreOptRef(PosToCell(parentAddr, pos));
            
            // transfer body
            var builder = new CellBuilder();
            builder.StoreUInt(0x0f8a7ea5, 32);              // transfer
            builder.StoreUInt(queryId, 64);
            builder.StoreCoins(new Coins(amount, new CoinsOptions(IsNano: true)));              // jetton_amount
            builder.StoreAddress(new Address(marketingAddr));   // destination
            builder.StoreAddress(new Address(senderAddr));      // response_address
            builder.StoreUInt(0, 1);                        // custom_payload:(Maybe ^Cell)
            builder.StoreCoins(new Coins(fee));                 // forward_ton_amount:Coins
            builder.StoreUInt(1, 1);                        // forward_payload:(Either Cell ^Cell)
            builder.StoreRef(fpBuilder.Build());
            
            return Result<BuyPlaceByJettonBodyResponse>.Success(new BuyPlaceByJettonBodyResponse
            {
                BocHex = builder.Build().ToString("hex").ToLower()
            });
        }
        catch (Exception e)
        {
            return Result<BuyPlaceByJettonBodyResponse>.Error(e.Message);
        }
    }

    // lock_pos#936ecf92  query_id:uint64  m:uint8  profile:Address  pos:^PlacePos = MarketingInternalMsg;
    public Result<LockPosBodyResponse> BuildLockPosBody(long queryId, int m, string profileAddr, string parentAddr, int pos)
    {
        try
        {
            var builder = new CellBuilder();
            builder.StoreUInt(0x936ecf92, 32); // lock_pos
            builder.StoreUInt(queryId, 64);
            builder.StoreUInt(m, 8);
            builder.StoreAddress(new Address(profileAddr));
            builder.StoreRef(PosToCell(parentAddr, pos));
            
            return Result<LockPosBodyResponse>.Success(new LockPosBodyResponse
            {
                BocHex = builder.Build().ToString("hex").ToLower()
            });
        }
        catch (Exception e)
        {
            return Result<LockPosBodyResponse>.Error(e.Message);
        }
    }
    
    // unlock_pos#ce87058b  query_id:uint64  m:uint8  profile:Address  pos:^PlacePos = MarketingInternalMsg;
    public Result<UnlockPosBodyResponse> BuildUnlockPosBody(long queryId, int m, string profileAddr, string parentAddr, int pos)
    {
        try
        {
            var builder = new CellBuilder();
            builder.StoreUInt(0xce87058b, 32); // unlock_pos
            builder.StoreUInt(queryId, 64);
            builder.StoreUInt(m, 8);
            builder.StoreAddress(new Address(profileAddr));
            builder.StoreRef(PosToCell(parentAddr, pos));
            
            return Result<UnlockPosBodyResponse>.Success(new UnlockPosBodyResponse
            {
                BocHex = builder.Build().ToString("hex").ToLower()
            });
        }
        catch (Exception e)
        {
            return Result<UnlockPosBodyResponse>.Error(e.Message);
        }
    }
    

    public async Task<Result<FirstTaskResponse>> GetFirstTaskAsync(string marketingAddr, CancellationToken ct = default)
    {
        try
        {
            var result = await tonClient.RunGetMethod(
                new Address(marketingAddr),
                "get_first_task",
                Array.Empty<IStackItem>());

            if (result is null)
                return Result<FirstTaskResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<FirstTaskResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            var key = result.Value.Stack.TryGetStruct<BigInteger>(0);
            var valCell = result.Value.Stack.TryGetClass<Cell>(1);
            var flag = (BigInteger)result.Value.Stack[2];

            return Result.Success(new FirstTaskResponse
            {
                Key = key is null ? null : (int)key.Value,
                Val = MarketingTaskFromCell(valCell),
                Flag = (int)flag
            });
        }
        catch (Exception exc)
        {
            return Result<FirstTaskResponse>.Error(exc.Message);
        }
    }

    public async Task<Result<MarketingDataResponse>> GetMarketingDataAsync(string marketingAddr, CancellationToken ct = default)
    {
        try
        {
            var result = await tonClient.RunGetMethod(
                new Address(marketingAddr),
                "get_marketing_data",
                Array.Empty<IStackItem>());

            if (result is null)
                return Result<MarketingDataResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<MarketingDataResponse>.Error(nameof(ContractErrors.GetMethodFailed));
            
            // Stack layout (from MarketingContract):
            // 0 admin_addr (cell->address)
            // 1 index
            // 2 max_tasks
            // 3 queue_size
            // 4 seq_no
            // 5 processor_addr (cell->address)
            // 6 jetton_wallet_addr (cell->address)
            // 7 initial_fee
            // 8 queue dict cell (optional)
            // 9 matrixes dict cell (optional)
            // 10 fees dict cell (optional)
            // 11 params cell
            
            var adminAddr = ((Cell)result.Value.Stack[0]).Parse().LoadAddress()!.ToString();
            var index = (BigInteger)result.Value.Stack[1];
            var maxTasks = (BigInteger)result.Value.Stack[2];
            var queueSize = (BigInteger)result.Value.Stack[3];
            var seqNo = (BigInteger)result.Value.Stack[4];
            var processorAddr = ((Cell)result.Value.Stack[5]).Parse().LoadAddress()!.ToString();
            var jettonWalletAddr = ((Cell)result.Value.Stack[6]).Parse().LoadAddress()?.ToString();
            var initialFee = (BigInteger)result.Value.Stack[7];
            var queueCell = result.Value.Stack.TryGetClass<Cell>(8);
            var matrixesCell = result.Value.Stack.TryGetClass<Cell>(9);
            var feesCell = result.Value.Stack.TryGetClass<Cell>(10);
            var paramsCell = (Cell)result.Value.Stack[11];
            
            var queue = QueueFromCell(queueCell);
            var matrixes = MatrixesFromCell(matrixesCell);
            var fees = FeesFromCell(feesCell);
            var @params = ParamsFromCell(paramsCell);

            return Result.Success(new MarketingDataResponse
            {
               AdminAddr = adminAddr,
               Index = (uint)index,
               MaxTasks = (uint)maxTasks,
               QueueSize = (uint)queueSize,
               SeqNo = (uint)seqNo,
               ProcessorAddr = processorAddr,
               JettonWalletAddr = jettonWalletAddr,
               InitialFee = (ulong)initialFee,
               Queue = queue,
               Matrixes = matrixes,
               Fees = fees,
               Params =  @params
            });
        }
        catch (Exception exc)
        {
            return Result<MarketingDataResponse>.Error(exc.Message);
        }
    }

    private static Dictionary<uint, MarketingTaskResponse> QueueFromCell(Cell? cell)
    {
        var result = new Dictionary<uint, MarketingTaskResponse>();
        if (cell is null)
            return result;

        var dict = Hashmap<Bits, Cell>.Deserialize(cell, GetDictOptions(32));
        
        var end = (byte)dict.Count;
        for (byte key = 1; key <= end; key++)
        {
            var keyBits = UIntToBits(key);

            var valCell = dict.Get(keyBits);
            if (valCell is null) continue;

            var val = MarketingTaskFromCell(valCell);
            if (val is null) continue;
            
            result.Add(key, val);
        }

        return result;
    }
    
  
    
    private static Dictionary<byte, MatrixConfigResponse> MatrixesFromCell(Cell? cell)
    {
        var result = new Dictionary<byte, MatrixConfigResponse>();
        if (cell is null)
            return result;

        var dict = Hashmap<Bits, Cell>.Deserialize(cell, GetDictOptions(8));
        
        var end = (byte)dict.Count;
        for (byte key = 1; key <= end; key++)
        {
            var keyBits = ByteToBits(key);

            var valCell = dict.Get(keyBits);
            if (valCell is null) continue;

            var val = MatrixConfigFromCell(valCell);
            if (val is null) continue;
            
            result.Add(key, val);
        }

        return result;
    }
    
    private static Dictionary<byte, decimal> FeesFromCell(Cell? cell)
    {
        var result = new Dictionary<byte, decimal>();
        if (cell is null)
            return result;

        var dict = Hashmap<Bits, Cell>.Deserialize(cell, GetDictOptions(8));
        
        var end = (byte)dict.Count;
        for (byte key = 1; key <= end; key++)
        {
            var keyBits = ByteToBits(key);

            var valCell = dict.Get(keyBits);
            if (valCell is null) continue;

            var val = FeeFromCell(valCell);
            if (val is null) continue;
            
            result.Add(key, val.Value);
        }

        return result;
    }

    private static Dictionary<byte,IEnumerable<RewardResponse>> RewardsConfigFromCell(Cell? cell)
    {
        var result = new Dictionary<byte, IEnumerable<RewardResponse>>();
        if (cell is null) 
            return result;
        
        var dict = Hashmap<Bits, Cell>.Deserialize(cell, GetDictOptions(8));
        
        var end = (byte)dict.Count;
        for (byte key = 1; key <= end; key++)
        {
            var keyBits = ByteToBits(key);

            var valCell = dict.Get(keyBits);
            if (valCell is null) continue;

            var val = RewardsFromCell(valCell);

            result.Add(key, val);
        }

        return result;
    }
    
    
    private static MarketingParamsResponse ParamsFromCell(Cell? cell)
    {
        // todo;
        return new MarketingParamsResponse();
    }
    
    private static HashmapOptions<Bits, Cell> GetDictOptions(uint keySize) => new()
    {
        KeySize = keySize,
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
    
    
    
    private static Bits UIntToBits(uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return new Bits(bytes);
    }
    
    private static Bits ByteToBits(byte value)
    {
        var bytes = new byte[] { value };
        return new Bits(bytes);
    }
    
   
    
    
    // _#_ query_id: uint64  m: uint8  profile: MsgAddress  payload: MarketingTaskPayload = MarketingTask;
    private static MarketingTaskResponse? MarketingTaskFromCell(Cell? cell)
    {
        if (cell is null) return null;

        var slice = cell.Parse();

        var queryId = (ulong)slice.LoadUInt(64);
        var m = (byte)slice.LoadUInt(8);
        var profileAddr = slice.LoadAddress()!.ToString();

        var payload = MarketingTaskPayloadFromSlice(slice);

        return new MarketingTaskResponse
        {
            QueryId = queryId,
            M = m,
            ProfileAddr = profileAddr,
            Payload = payload
        };
    }

    private static ulong? FeeFromCell(Cell? cell)
    {
        if (cell is null) return null;

        var slice = cell.Parse();
        var fee = slice.LoadCoins().ToBigInt();
        return (ulong)fee;
    }

    /*
    _#_ price: Coins
        owner_address: MsgAddress
        royalty_numerator: uint16
        royalty_denominator: uint16
        width: uint8,
        height: uint8,
        code: ^Cell,
        rewards (HashmapE 8 RewardConfig)
        name: Cell = MatrixConfig;
*/

    private static MatrixConfigResponse? MatrixConfigFromCell(Cell? cell)
    {
        if (cell is null) return null;
        
        var slice = cell.Parse();
        var price = slice.LoadCoins().ToBigInt();
        var ownerAddr = slice.LoadAddress()!.ToString();
        var royaltyNumerator = (ushort)slice.LoadUInt(16);
        var royaltyDenominator = (ushort)slice.LoadUInt(16);
        var with = (byte)slice.LoadUInt(8);
        var height = (byte)slice.LoadUInt(8);
        var code = slice.LoadRef().ToString();
        var rewardsCell = slice.LoadOptRef();
        var name = slice.LoadString();
        
        var rewards = RewardsConfigFromCell(rewardsCell);
            
        return new MatrixConfigResponse
        {
            Price = (ulong)price,
            OwnerAddr = ownerAddr,
            RoyaltyNumerator = royaltyNumerator,
            RoyaltyDenominator = royaltyDenominator,
            Code = code,
            Width = with,
            Height = height,
            Rewards = rewards,
            Name = name
        };
    }

    private static IEnumerable<RewardResponse> RewardsFromCell(Cell? cell)
    {
        if (cell is null) yield break;
        
        var slice = cell.Parse();
        
        while (slice.RemainderBits > 0)
        {
            yield return DeserializeReward(slice);
        }
    }

    private static RewardResponse DeserializeReward(CellSlice s)
    {
        var tag = (byte)s.LoadUInt(4);

        return tag switch
        {
            1 => new RewardResponse { Tag = "1__clone", M = (byte)s.LoadUInt(8), Count = (byte)s.LoadUInt(8) },
            2 => new RewardResponse { Tag = "2__reinvest" },
            3 => new RewardResponse { Tag = "3__struct_bonus", Amount = (ulong)s.LoadCoins().ToBigInt() },
            4 => new RewardResponse { Tag = "4__ref_bonus", Amount = (ulong)s.LoadCoins().ToBigInt() },
            5 => new RewardResponse { Tag = "5__dev_bonus", Amount = (ulong)s.LoadCoins().ToBigInt() },
            6 => new RewardResponse { Tag = "6__move__or__bonus", Amount = (ulong)s.LoadCoins().ToBigInt() },
            _ => throw new NotSupportedException("Unknow tag")
        };
    }


    private static MarketingTaskPayloadResponse MarketingTaskPayloadFromSlice(CellSlice slice)
    {
        var tag = (byte)slice.LoadUInt(4);

        return tag switch
        {
            // buy_place#1  source: MsgAddress  amount: Coins  first:Bool  pos: (Maybe ^PlacePos) = MarketingTaskPayload;
            1 => new MarketingTaskPayloadResponse
            {
                Tag = tag,
                SourceAddr = slice.LoadAddress()!.ToString(),
                Amount = (ulong)slice.LoadCoins().ToBigInt(),
                First = slice.LoadBit(),
                Pos = PosFromCell(slice.LoadOptRef())
            },
            
            // create_clone#2 = MarketingTaskPayload;
            2 => new MarketingTaskPayloadResponse
            {
                Tag = tag
            },
            
            // lock_pos#3  source:MsgAddress  pos:^PlacePos = MarketingTaskPayload;
            3 => new MarketingTaskPayloadResponse
            {
                Tag = tag,
                SourceAddr = slice.LoadAddress()!.ToString(),
                Pos = PosFromCell(slice.LoadRef())
            },
            
            // unlock_pos#4  source:MsgAddress  pos:^PlacePos = MarketingTaskPayload;
            4 => new MarketingTaskPayloadResponse
            {
                Tag = tag,
                SourceAddr = slice.LoadAddress()!.ToString(),
                Pos = PosFromCell(slice.LoadRef())
            },
            
            // jetton_bonus#5  amount:Coins  place_number:#  title:Any = MarketingTaskPayload;
            5 => new MarketingTaskPayloadResponse
            {
                Tag = tag,
                Amount = (ulong)slice.LoadCoins().ToBigInt(),
                PlaceNumber =(uint?) slice.LoadUInt(32),
                Title = slice.LoadString()
            },
            
            // reinvest#6 = MarketingTaskPayload;
            6 => new MarketingTaskPayloadResponse
            {
                Tag = tag
            },
            
            // move_or_bonus#7  amount:Coins  place_number:#  title:Any = MarketingTaskPayload;
            7 => new MarketingTaskPayloadResponse
            {
                Tag = tag,
                Amount = (ulong)slice.LoadCoins().ToBigInt(),
                PlaceNumber =(uint?) slice.LoadUInt(32),
                Title = slice.LoadString()
            },
            _ => throw new NotImplementedException($"Unknown MarketingTask payload tag: {tag}")
        };
    }
}