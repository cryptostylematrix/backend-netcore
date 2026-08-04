using Contracts.Infrastructure.Helpers;
using Contracts.Infrastructure.Ton;

namespace Contracts.Infrastructure.Queries;

public sealed class MarketingV3Queries(ITonClient tonClient) : IMarketingV3Queries
{
    private const uint ExecTag = 0x748e0264;
    private const uint CommandResponseTag = 0xc328f3f3;
    private const uint BonusQueryResponseTag = 0x96e7f3f6;
    private const uint ProfileInfoQueryResponseTag = 0x6d7fa64d;
    private const uint CancelTaskTag = 0x87d5330c;

    private const uint UserCommandTaskTag = 0x48b18cdf;
    private const uint SystemCommandTaskTag = 0x5bfcb9f2;
    private const uint BonusQueryTaskTag = 0x24a2cffa;
    private const uint ProfileInfoQueryTaskTag = 0xf1e4dc7b;

    private const uint CommandsRewardTag = 0x210bbdce;
    private const uint BonusRewardTag = 0xc4a6ef3e;
    private const uint CommandOrBonusRewardTag = 0xfb0f2b7c;
    private const uint ProfilePaymentRewardTag = 0x33a40e44;
    private const uint DirectPaymentRewardTag = 0x67d146f6;

    public Result<MarketingV3MessageBodyResponse> BuildExecMessageBody(
        ulong queryId,
        byte structure,
        string profileAddr,
        uint commandTag,
        string? payloadBocHex)
    {
        return BuildBody(builder =>
        {
            builder.StoreUInt(ExecTag, 32);
            builder.StoreUInt(queryId, 64);
            builder.StoreUInt(structure, 8);
            builder.StoreAddress(new Address(profileAddr));
            builder.StoreUInt(commandTag, 32);
            builder.StoreOptRef(string.IsNullOrWhiteSpace(payloadBocHex)
                ? null
                : Cell.From(new Bits(Convert.FromHexString(payloadBocHex))));
        });
    }

    public Result<MarketingV3MessageBodyResponse> SendCommandResponse(
        ulong queryId,
        uint taskKey,
        uint code,
        MarketingV3SourcePlace source)
    {
        return BuildBody(builder =>
        {
            builder.StoreUInt(CommandResponseTag, 32);
            builder.StoreUInt(queryId, 64);
            builder.StoreUInt(taskKey, 32);
            builder.StoreUInt(code, 32);
            StoreSourcePlace(builder, source);
        });
    }

    public Result<MarketingV3MessageBodyResponse> SendBonusQueryResponse(
        ulong queryId,
        uint taskKey,
        MarketingV3PlaceInfo reason,
        MarketingV3ProfileData recipient)
    {
        return BuildBody(builder =>
        {
            builder.StoreUInt(BonusQueryResponseTag, 32);
            builder.StoreUInt(queryId, 64);
            builder.StoreUInt(taskKey, 32);
            StorePlaceInfo(builder, reason);
            StoreProfileData(builder, recipient);
        });
    }

    public Result<MarketingV3MessageBodyResponse> SendProfileInfoQueryResponse(
        ulong queryId,
        uint taskKey,
        MarketingV3ProfileInfo profile)
    {
        return BuildBody(builder =>
        {
            builder.StoreUInt(ProfileInfoQueryResponseTag, 32);
            builder.StoreUInt(queryId, 64);
            builder.StoreUInt(taskKey, 32);
            builder.StoreRef(StringCell(profile.ProfileLogin));
            builder.StoreAddress(ParseAddress(profile.OwnerAddr));
        });
    }

    public Result<MarketingV3MessageBodyResponse> SendCancelTask(
        ulong queryId,
        uint taskKey,
        string comment)
    {
        return BuildBody(builder =>
        {
            builder.StoreUInt(CancelTaskTag, 32);
            builder.StoreUInt(queryId, 64);
            builder.StoreUInt(taskKey, 32);
            builder.StoreRef(StringCell(comment));
        });
    }

    public async Task<Result<MarketingV3DataResponse>> GetMarketingDataAsync(
        string marketingAddr,
        CancellationToken ct = default)
    {
        try
        {
            var result = await tonClient.RunGetMethod(
                new Address(marketingAddr),
                "get_marketing_data",
                Array.Empty<IStackItem>());

            if (result is null)
                return Result<MarketingV3DataResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<MarketingV3DataResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            var stack = result.Value.Stack;
            var queueCell = stack.TryGetClass<Cell>(8);
            var structuresCell = stack.TryGetClass<Cell>(9);

            return Result.Success(new MarketingV3DataResponse
            {
                AdminAddr = AddressFromStack(stack[0]),
                Index = ToUInt32(stack[1]),
                SeriesTag = ToUInt32(stack[2]),
                MetadataUri = StringFromCell((Cell)stack[3]),
                MaxTasks = ToUInt16(stack[4]),
                QueueSize = ToUInt16(stack[5]),
                SeqNo = ToUInt32(stack[6]),
                ProcessorAddr = AddressFromStack(stack[7]),
                Queue = DictionaryFromCell(queueCell, 32, BitsToUInt32, MarketingTaskFromCell),
                Structures = DictionaryFromCell(structuresCell, 8, BitsToByte, StructureConfigFromCell),
                PrefixBocHex = CellToBocHex((Cell)stack[10])
            });
        }
        catch (Exception exc)
        {
            return Result<MarketingV3DataResponse>.Error(exc.Message);
        }
    }

    public async Task<Result<MarketingV3FirstTaskResponse>> GetFirstTaskAsync(
        string marketingAddr,
        CancellationToken ct = default)
    {
        try
        {
            var result = await tonClient.RunGetMethod(
                new Address(marketingAddr),
                "get_first_task",
                Array.Empty<IStackItem>());

            if (result is null)
                return Result<MarketingV3FirstTaskResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<MarketingV3FirstTaskResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            var key = result.Value.Stack.TryGetStruct<BigInteger>(0);
            var taskCell = result.Value.Stack.TryGetClass<Cell>(1);

            return Result.Success(new MarketingV3FirstTaskResponse
            {
                Key = key is null ? null : checked((uint)key.Value),
                Val = taskCell is null ? null : MarketingTaskFromCell(taskCell),
                Flag = checked((int)(BigInteger)result.Value.Stack[2])
            });
        }
        catch (Exception exc)
        {
            return Result<MarketingV3FirstTaskResponse>.Error(exc.Message);
        }
    }

    public async Task<Result<MarketingV3BasicDataResponse>> GetBasicDataAsync(
        string marketingAddr,
        CancellationToken ct = default)
    {
        try
        {
            var result = await tonClient.RunGetMethod(
                new Address(marketingAddr),
                "get_basic_data",
                Array.Empty<IStackItem>());

            if (result is null)
                return Result<MarketingV3BasicDataResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<MarketingV3BasicDataResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            var stack = result.Value.Stack;
            return Result.Success(new MarketingV3BasicDataResponse
            {
                Init = checked((int)(BigInteger)stack[0]),
                AdminAddr = AddressFromStack(stack[1]),
                Index = ToUInt32(stack[2]),
                SeriesTag = ToUInt32(stack[3]),
                MetadataUri = StringFromOptionalCell(stack.TryGetClass<Cell>(4))
            });
        }
        catch (Exception exc)
        {
            return Result<MarketingV3BasicDataResponse>.Error(exc.Message);
        }
    }

    private static Result<MarketingV3MessageBodyResponse> BuildBody(Action<CellBuilder> write)
    {
        try
        {
            var builder = new CellBuilder();
            write(builder);

            return Result.Success(new MarketingV3MessageBodyResponse
            {
                BocHex = CellToBocHex(builder.Build())
            });
        }
        catch (Exception exc)
        {
            return Result<MarketingV3MessageBodyResponse>.Error(exc.Message);
        }
    }

    private static void StoreSourcePlace(CellBuilder builder, MarketingV3SourcePlace source)
    {
        StorePlaceRef(builder, source.Place);
        builder.StoreOptRef(source.ProfileLogin is null ? null : StringCell(source.ProfileLogin));
    }

    private static void StorePlaceRef(CellBuilder builder, MarketingV3PlaceRef place)
    {
        builder.StoreUInt(place.Struct, 8);
        builder.StoreAddress(ParseAddress(place.ProfileAddr));
        builder.StoreUInt(place.PlaceNumber, 32);
    }

    private static void StorePlaceInfo(CellBuilder builder, MarketingV3PlaceInfo place)
    {
        builder.StoreUInt(place.PlaceNumber, 32);
        builder.StoreOptRef(place.ProfileLogin is null ? null : StringCell(place.ProfileLogin));
    }

    private static void StoreProfileData(CellBuilder builder, MarketingV3ProfileData profile)
    {
        builder.StoreAddress(ParseAddress(profile.ProfileAddr));
        builder.StoreRef(StringCell(profile.ProfileLogin));
        builder.StoreAddress(ParseAddress(profile.OwnerAddr));
    }

    private static Address? ParseAddress(string? address) =>
        string.IsNullOrWhiteSpace(address) ? null : new Address(address);

    private static Cell StringCell(string value) =>
        new CellBuilder().StoreStringTail(value).Build();

    private static MarketingV3TaskResponse MarketingTaskFromCell(Cell cell)
    {
        var slice = cell.Parse();
        var queryId = checked((ulong)slice.LoadUInt(64));
        var commandCell = slice.LoadOptRef();
        var queryCell = slice.LoadOptRef();
        var payloadCell = slice.LoadOptRef();

        return new MarketingV3TaskResponse
        {
            QueryId = queryId,
            Command = commandCell is null ? null : TaskCommandFromCell(commandCell),
            Query = queryCell is null ? null : TaskQueryFromCell(queryCell),
            PayloadBocHex = payloadCell is null ? null : CellToBocHex(payloadCell)
        };
    }

    private static MarketingV3TaskCommandResponse TaskCommandFromCell(Cell cell)
    {
        var slice = cell.Parse();
        var tag = checked((uint)slice.LoadUInt(32));

        return tag switch
        {
            UserCommandTaskTag => new MarketingV3TaskCommandResponse
            {
                Tag = tag,
                Struct = checked((byte)slice.LoadUInt(8)),
                CommandTag = checked((uint)slice.LoadUInt(32)),
                ProfileAddr = slice.LoadAddress()?.ToString(),
                SourceAddr = slice.LoadAddress()?.ToString(),
                Amount = checked((ulong)slice.LoadCoins().ToBigInt()),
                SenderJettonWallet = slice.LoadAddress()?.ToString()
            },
            SystemCommandTaskTag => new MarketingV3TaskCommandResponse
            {
                Tag = tag,
                CommandStruct = checked((byte)slice.LoadUInt(8)),
                CommandTag = checked((uint)slice.LoadUInt(32)),
                Relative = RelativePlaceRefFromSlice(slice)
            },
            _ => throw new NotSupportedException($"Unknown MarketingTaskCommand tag: 0x{tag:x8}")
        };
    }

    private static MarketingV3TaskQueryResponse TaskQueryFromCell(Cell cell)
    {
        var slice = cell.Parse();
        var tag = checked((uint)slice.LoadUInt(32));

        return tag switch
        {
            BonusQueryTaskTag => new MarketingV3TaskQueryResponse
            {
                Tag = tag,
                BonusTypeTag = checked((uint)slice.LoadUInt(32)),
                Relative = RelativePlaceRefFromSlice(slice),
                Amount = checked((ulong)slice.LoadCoins().ToBigInt()),
                SenderJettonWallet = slice.LoadAddress()?.ToString(),
                BonusTitle = StringFromCell(slice.LoadRef())
            },
            ProfileInfoQueryTaskTag => new MarketingV3TaskQueryResponse
            {
                Tag = tag,
                Struct = checked((byte)slice.LoadUInt(8)),
                BonusTypeTag = checked((uint)slice.LoadUInt(32)),
                Reason = PlaceInfoFromSlice(slice),
                RecipientProfileAddr = slice.LoadAddress()?.ToString(),
                Amount = checked((ulong)slice.LoadCoins().ToBigInt()),
                SenderJettonWallet = slice.LoadAddress()?.ToString(),
                BonusTitle = StringFromCell(slice.LoadRef())
            },
            _ => throw new NotSupportedException($"Unknown MarketingTaskQuery tag: 0x{tag:x8}")
        };
    }

    private static MarketingV3PlaceRef PlaceRefFromSlice(CellSlice slice) => new()
    {
        Struct = checked((byte)slice.LoadUInt(8)),
        ProfileAddr = slice.LoadAddress()?.ToString(),
        PlaceNumber = checked((uint)slice.LoadUInt(32))
    };

    private static MarketingV3RelativePlaceRef RelativePlaceRefFromSlice(CellSlice slice) => new()
    {
        Source = PlaceRefFromSlice(slice),
        Level = checked((ushort)slice.LoadUInt(16))
    };

    private static MarketingV3PlaceInfo PlaceInfoFromSlice(CellSlice slice) => new()
    {
        PlaceNumber = checked((uint)slice.LoadUInt(32)),
        ProfileLogin = StringFromOptionalCell(slice.LoadOptRef())
    };

    private static MarketingV3StructureConfigResponse StructureConfigFromCell(Cell cell)
    {
        var slice = cell.Parse();
        var commandsCell = slice.LoadOptRef();
        var rewardsCell = slice.LoadOptRef();
        var royaltiesCell = slice.LoadOptRef();

        return new MarketingV3StructureConfigResponse
        {
            Commands = DictionaryFromCell(commandsCell, 32, BitsToUInt32, CommandConfigFromCell),
            Rewards = DictionaryFromCell(rewardsCell, 32, BitsToUInt32, RewardConfigFromCell),
            Royalties = DictionaryFromCell(royaltiesCell, 256, BitsToHex, RoyaltyConfigFromCell),
            Name = StringFromCell(slice.LoadRef())
        };
    }

    private static MarketingV3CommandConfigResponse CommandConfigFromCell(Cell cell)
    {
        var slice = cell.Parse();
        return new MarketingV3CommandConfigResponse
        {
            Price = checked((ulong)slice.LoadCoins().ToBigInt()),
            SenderJettonWallet = slice.LoadAddress()?.ToString(),
            GramFee = checked((ulong)slice.LoadCoins().ToBigInt())
        };
    }

    private static MarketingV3RewardConfigResponse RewardConfigFromCell(Cell cell)
    {
        var setsCell = cell.Parse().LoadOptRef();
        return new MarketingV3RewardConfigResponse
        {
            Sets = DictionaryFromCell(setsCell, 8, BitsToByte, RewardSetFromCell)
        };
    }

    private static IReadOnlyCollection<MarketingV3RewardResponse> RewardSetFromCell(Cell cell)
    {
        var rewards = new List<MarketingV3RewardResponse>();
        var slice = cell.Parse();

        while (slice.LoadBit())
        {
            rewards.Add(RewardFromSlice(slice));
            if (slice.LoadBit())
                slice = slice.LoadRef().Parse();
        }

        return rewards;
    }

    private static MarketingV3RewardResponse RewardFromSlice(CellSlice slice)
    {
        var tag = checked((uint)slice.LoadUInt(32));

        return tag switch
        {
            CommandsRewardTag => new MarketingV3RewardResponse
            {
                Tag = tag,
                FromLevel = checked((ushort)slice.LoadUInt(16)),
                ToLevel = checked((ushort)slice.LoadUInt(16)),
                Count = checked((byte)slice.LoadUInt(8)),
                Struct = checked((byte)slice.LoadUInt(8)),
                CommandTag = checked((uint)slice.LoadUInt(32)),
                PayloadBocHex = CellToOptionalBocHex(slice.LoadOptRef())
            },
            BonusRewardTag => new MarketingV3RewardResponse
            {
                Tag = tag,
                FromLevel = checked((ushort)slice.LoadUInt(16)),
                ToLevel = checked((ushort)slice.LoadUInt(16)),
                BonusTypeTag = checked((uint)slice.LoadUInt(32)),
                Amount = checked((ulong)slice.LoadCoins().ToBigInt()),
                SenderJettonWallet = slice.LoadAddress()?.ToString(),
                Title = StringFromCell(slice.LoadRef()),
                PayloadBocHex = CellToOptionalBocHex(slice.LoadOptRef())
            },
            CommandOrBonusRewardTag => new MarketingV3RewardResponse
            {
                Tag = tag,
                FromLevel = checked((ushort)slice.LoadUInt(16)),
                ToLevel = checked((ushort)slice.LoadUInt(16)),
                CommandStruct = checked((byte)slice.LoadUInt(8)),
                CommandTag = checked((uint)slice.LoadUInt(32)),
                BonusTypeTag = checked((uint)slice.LoadUInt(32)),
                Amount = checked((ulong)slice.LoadCoins().ToBigInt()),
                SenderJettonWallet = slice.LoadAddress()?.ToString(),
                Title = StringFromCell(slice.LoadRef()),
                PayloadBocHex = CellToOptionalBocHex(slice.LoadOptRef())
            },
            ProfilePaymentRewardTag => new MarketingV3RewardResponse
            {
                Tag = tag,
                BonusTypeTag = checked((uint)slice.LoadUInt(32)),
                ProfileAddr = slice.LoadAddress()?.ToString(),
                Amount = checked((ulong)slice.LoadCoins().ToBigInt()),
                SenderJettonWallet = slice.LoadAddress()?.ToString(),
                Title = StringFromCell(slice.LoadRef()),
                PayloadBocHex = CellToOptionalBocHex(slice.LoadOptRef())
            },
            DirectPaymentRewardTag => new MarketingV3RewardResponse
            {
                Tag = tag,
                Recipient = slice.LoadAddress()?.ToString(),
                Amount = checked((ulong)slice.LoadCoins().ToBigInt()),
                SenderJettonWallet = slice.LoadAddress()?.ToString(),
                ForwardTonAmount = checked((ulong)slice.LoadCoins().ToBigInt()),
                Title = StringFromCell(slice.LoadRef())
            },
            _ => throw new NotSupportedException($"Unknown Reward tag: 0x{tag:x8}")
        };
    }

    private static MarketingV3RoyaltyConfigResponse RoyaltyConfigFromCell(Cell cell)
    {
        var slice = cell.Parse();
        return new MarketingV3RoyaltyConfigResponse
        {
            Numerator = checked((ushort)slice.LoadUInt(16)),
            Denominator = checked((ushort)slice.LoadUInt(16)),
            Recipient = slice.LoadAddress()?.ToString()
        };
    }

    private static Dictionary<TKey, TValue> DictionaryFromCell<TKey, TValue>(
        Cell? cell,
        uint keySize,
        Func<Bits, TKey> parseKey,
        Func<Cell, TValue> parseValue)
        where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>();
        if (cell is null)
            return result;

        var options = CellDictionaryOptions(keySize);
        var dictionary = Hashmap<Bits, Cell>.Deserialize(cell, options);
        foreach (var item in dictionary.GetEntries(options))
            result.Add(parseKey(item.Key), parseValue(item.Value));

        return result;
    }

    private static HashmapOptions<Bits, Cell> CellDictionaryOptions(uint keySize) => new()
    {
        KeySize = keySize,
        Serializers = new HashmapSerializers<Bits, Cell>
        {
            Key = bits => bits,
            Value = value => value
        },
        Deserializers = new HashmapDeserializers<Bits, Cell>
        {
            Key = bits => bits,
            Value = value => value
        }
    };

    private static byte BitsToByte(Bits bits) => checked((byte)BitsToBigInteger(bits));

    private static uint BitsToUInt32(Bits bits) => checked((uint)BitsToBigInteger(bits));

    private static BigInteger BitsToBigInteger(Bits bits) =>
        new(bits.ToBytes(), isUnsigned: true, isBigEndian: true);

    private static string BitsToHex(Bits bits) =>
        Convert.ToHexString(bits.ToBytes()).ToLowerInvariant();

    private static ushort ToUInt16(object item) => checked((ushort)(BigInteger)item);

    private static uint ToUInt32(object item) => checked((uint)(BigInteger)item);

    private static string AddressFromStack(object item) =>
        ((Cell)item).Parse().LoadAddress()?.ToString()
        ?? throw new InvalidOperationException("Expected a non-null address.");

    private static string? StringFromOptionalCell(Cell? cell) =>
        cell is null ? null : StringFromCell(cell);

    private static string StringFromCell(Cell cell)
    {
        var value = new StringBuilder();
        var slice = cell.Parse();

        while (true)
        {
            value.Append(slice.LoadString());
            if (slice.RemainderRefs == 0)
                return value.ToString();

            slice = slice.LoadRef().Parse();
        }
    }

    private static string? CellToOptionalBocHex(Cell? cell) =>
        cell is null ? null : CellToBocHex(cell);

    private static string CellToBocHex(Cell cell) =>
        cell.ToString("hex").ToLowerInvariant();
}
