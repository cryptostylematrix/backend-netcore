using System.Security.Cryptography;
using Contracts.Infrastructure.NftContent;
using static Contracts.Infrastructure.Caching.CacheEntryOptions;

namespace Contracts.Infrastructure.Queries;

public sealed class ProfileItemQueries(
    ITonClient tonClient,
    IDistributedCache cache,
    IOptions<TonQueryCacheOptions> cacheOpts) : IProfileItemQueries
{
    private readonly TonQueryCacheOptions _cacheOpts = cacheOpts.Value;
    
    public Task<Result<ProfileDataResponse>> GetNftDataAsync(string addr, CancellationToken ct = default)
    {
        var normalizedAddr = new Address(addr).ToString();
        var key = $"{_cacheOpts.KeyPrefix}:profileItem:nftData:{normalizedAddr}";

        return CacheGetOrFetch.GetOrFetchAsync(
            cache,
            key,
            fetch: _ => FetchNftDataAsync(normalizedAddr),
            shouldCache: dto => dto is
            {
                IsInit: -1,
                Content:
                {
                    FirstName: not null, 
                    LastName: not null, 
                    ImageUrl: not null, 
                    TgUsername: not null
                }
            },
            options: TtlDays(_cacheOpts.NftDataIsInitMinusOneTtlDays),
            ct: ct);
    }


    private async Task<Result<ProfileDataResponse>> FetchNftDataAsync(string addr)
    {
        try
        {
            var result = await tonClient.RunGetMethod(
                new Address(addr),
                "get_nft_data",
                Array.Empty<IStackItem>());

            if (result is null)
                return Result<ProfileDataResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<ProfileDataResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            // Domain mapping equivalence:
            // IsInit = (BigInteger)stack[0]  -> DTO int
            // Index  = (BigInteger)stack[1]  -> DTO string (you currently have string)
            // CollectionAddr = address.ToString()
            // OwnerAddr = nullable address.ToString()
            // Content -> ProfileContentResponse? (same normalization logic)

            var isInit = (BigInteger)result.Value.Stack[0];
            var index = (BigInteger)result.Value.Stack[1];

            var collection = ((Cell)result.Value.Stack[2]).Parse().ReadAddress()!.ToString();

            var ownerCell = result.Value.Stack.TryGetClass<Cell>(3);
            var owner = ownerCell?.Parse().ReadAddress()?.ToString(); // preserves null

            var contentCell = result.Value.Stack.TryGetClass<Cell>(4);
            var content = ProfileContentFromCell(contentCell);

            return Result.Success(new ProfileDataResponse
            {
                IsInit = (int)isInit,          // matches your DTO type
                Index = index.ToString(),      // matches your DTO type + safe
                CollectionAddr = collection,
                OwnerAddr = owner,
                Content = content
            });
        }
        catch (Exception exc)
        {
            return Result<ProfileDataResponse>.Error(exc.Message);
        }
    }
    
    public Task<Result<ProfileProgramsResponse>> GetProgramsAsync(string addr, CancellationToken ct = default)
    {
        var normalizedAddr = new Address(addr).ToString();
        var key = $"{_cacheOpts.KeyPrefix}:profileItem:programs:{normalizedAddr}";

        return CacheGetOrFetch.GetOrFetchAsync(
            cache,
            key,
            fetch: _ => FetchProgramsAsync(normalizedAddr),
            shouldCache: dto => dto.Multi is not null && dto.Multi.Confirmed == 1,
            options: TtlDays(_cacheOpts.LongTtlDays),
            ct: ct);
    }

    public Result<MultiChooseInviterBodyResponse> BuildChooseInviterBody(long queryId, int program, string inviterAddr, int seqNo, string inviteAddr)
    {
        try
        {
            var builder = new CellBuilder();
            builder.StoreUInt(0xef27e2d6, 32); // ITEM_CHOOSE_INVITER
            builder.StoreUInt(queryId, 64);
            builder.StoreUInt(program, 32);
            builder.StoreAddress(new Address(inviterAddr));
            builder.StoreUInt(seqNo, 32);
            builder.StoreAddress(new Address(inviteAddr));
            return Result<MultiChooseInviterBodyResponse>.Success(new MultiChooseInviterBodyResponse
            {
                BocHex = builder.Build().ToString("hex").ToLower()
            });
        }
        catch (Exception e)
        {
            return Result<MultiChooseInviterBodyResponse>.Error(e.Message);
        }
    }

    public Result<EditContentBodyResponse> BuildEditContentBody(long queryId, string login, string? imageUrl, string? firstName, string? lastName,
        string? tgUsername)
    {
        try
        {
            var content = ProfileNftContent.ProfileToNftContent(
                login: login,
                imageUrl: imageUrl,
                firstName: firstName,
                lastName: lastName,
                tgUsername: tgUsername);
            
            var contentCell = NftContentWriter.NftContentToCell(content);
            
            var builder = new CellBuilder();
            builder.StoreUInt(0x1a0b9d51, 32); // Edit content op
            builder.StoreUInt(queryId, 64);
            builder.StoreRef(contentCell);
            
            return Result<EditContentBodyResponse>.Success(new EditContentBodyResponse
            {
                BocHex = builder.Build().ToString("hex").ToLower()
            });
        }
        catch (Exception e)
        {
            return Result<EditContentBodyResponse>.Error(e.Message);
        }
    }


    private async Task<Result<ProfileProgramsResponse>> FetchProgramsAsync(string addr)
    {
        try
        {
            var result = await tonClient.RunGetMethod(
                new Address(addr),
                "get_programs",
                Array.Empty<IStackItem>());

            if (result is null)
                return Result<ProfileProgramsResponse>.Error(nameof(ContractErrors.GetMethodReturnsNull));

            if (result.Value.ExitCode != 0)
                return Result<ProfileProgramsResponse>.Error(nameof(ContractErrors.GetMethodFailed));

            var programsCell = result.Value.Stack.TryGetClass<Cell>(0);
            var multi = ParseMultiProgram(programsCell);

            return Result.Success(new ProfileProgramsResponse
            {
                Multi = multi
            });
        }
        catch (Exception exc)
        {
            return Result<ProfileProgramsResponse>.Error(exc.Message);
        }
    }

    // -------------------------
    // Programs: dict parsing
    // -------------------------

    private static ProgramDataResponse? ParseMultiProgram(Cell? programsCell)
    {
        if (programsCell is null) return null;

        try
        {
            var dict = Hashmap<Bits, ProgramDataResponse>.Deserialize(programsCell, ProgramDictOptions);

            const uint multiHash = 0x1ce8c484;

            var bytes = BitConverter.GetBytes(multiHash);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);

            var key = new Bits(bytes);

            return dict.Get(key);
        }
        catch
        {
            return null;
        }
    }

    private static HashmapOptions<Bits, ProgramDataResponse> ProgramDictOptions => new()
    {
        KeySize = 32,
        Serializers = new HashmapSerializers<Bits, ProgramDataResponse>
        {
            Key = bits => bits,
            Value = _ => new CellBuilder().Build()
        },
        Deserializers = new HashmapDeserializers<Bits, ProgramDataResponse>
        {
            Key = bits => bits,
            Value = DeserializeProgramDataResponse
        }
    };

    private static ProgramDataResponse DeserializeProgramDataResponse(Cell cell)
    {
        var slice = cell.Parse();

        return new ProgramDataResponse
        {
            InviterAddr = slice.LoadAddress()!.ToString(),
            SeqNo = (uint)slice.LoadUInt(32),
            InviteAddr = slice.LoadAddress()!.ToString(),
            Confirmed = (uint)slice.LoadUInt(1)
        };
    }

    // -------------------------
    // Content parsing (ported)
    // -------------------------

    private const string DefaultImage = "https://cryptostylematrix.github.io/frontend/cs-big.png";

    private static ProfileContentResponse? ProfileContentFromCell(Cell? content)
    {
        if (content is null) return null;

        var fields = new Dictionary<string, string?>(StringComparer.Ordinal);

        try
        {
            var slice = content.Parse();
            var start = (uint)slice.LoadUInt(8);
            if (start != 0)
                throw new Exception("Unknown on-chain content format");

            var dict = slice.LoadDict(NftDictValueOptions);

            foreach (var key in new[] { "image", "name", "description", "attributes" })
            {
                var keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
                var dictValue = dict.Get(new Bits(keyHash));
                if (dictValue is not null)
                    fields[key] = Encoding.UTF8.GetString(dictValue.Content);
            }
        }
        catch
        {
            return null;
        }

        var attrs = ExtractAttributes(fields.GetValueOrDefault("attributes"));

        var imageUrl =
            ToLower(fields.GetValueOrDefault("image")) is { Length: > 0 } img
                ? img
                : DefaultImage;

        var login =
            SanitizeLogin(attrs.Login ?? fields.GetValueOrDefault("name"))
            ?? "unknown";

        return new ProfileContentResponse
        {
            Login = login,
            ImageUrl = imageUrl,
            FirstName = Capitalize(attrs.FirstName),
            LastName = Capitalize(attrs.LastName),
            TgUsername = ToLower(attrs.TgUsername)
        };
    }

    private sealed class NftDictValue
    {
        public required byte[] Content { get; init; }
    }

    private static HashmapOptions<Bits, NftDictValue> NftDictValueOptions => new()
    {
        KeySize = 256,
        Serializers = new HashmapSerializers<Bits, NftDictValue>
        {
            Key = bits => bits,
            Value = _ => new CellBuilder().Build()
        },
        Deserializers = new HashmapDeserializers<Bits, NftDictValue>
        {
            Key = bits => bits,
            Value = cell =>
            {
                var innerSlice = cell.Parse().LoadRef().Parse();
                var start = (byte)innerSlice.LoadUInt(8);

                return start switch
                {
                    0 => new NftDictValue { Content = FlattenSnakeCell(new CellBuilder().StoreCellSlice(innerSlice).Build()) },
                    1 => new NftDictValue { Content = ParseChunkDict(innerSlice) },
                    _ => new NftDictValue { Content = Array.Empty<byte>() }
                };
            }
        }
    };

    // Matches your Domain behavior: currently ParseChunkDict does not actually enumerate/concat chunks.
    // If you later implement enumeration in your SDK wrapper, update this.
    private static byte[] ParseChunkDict(CellSlice slice)
    {
        try
        {
            _ = slice.LoadDict(ChunkDictValueOptions);
            return Array.Empty<byte>();
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    private sealed class ChunkDictValue
    {
        public required byte[] Content { get; init; }
    }

    private static HashmapOptions<Bits, ChunkDictValue> ChunkDictValueOptions => new()
    {
        KeySize = 256,
        Serializers = new HashmapSerializers<Bits, ChunkDictValue>
        {
            Key = bits => bits,
            Value = _ => new CellBuilder().Build()
        },
        Deserializers = new HashmapDeserializers<Bits, ChunkDictValue>
        {
            Key = bits => bits,
            Value = cell =>
            {
                var snakeCell = cell.Parse().LoadRef();
                return new ChunkDictValue { Content = FlattenSnakeCell(snakeCell) };
            }
        }
    };

    private static byte[] FlattenSnakeCell(Cell cell)
    {
        var c = cell;
        var buffers = new List<byte[]>();

        while (c != null)
        {
            var slice = c.Parse();

            var bits = slice.RemainderBits;
            if (bits > 0)
            {
                var bytesToRead = bits / 8;
                if (bytesToRead > 0)
                    buffers.Add(slice.LoadBytes(bytesToRead));
            }

            c = slice.RemainderRefs > 0 ? slice.LoadRef() : null;
        }

        return Concat(buffers);
    }

    private static byte[] Concat(IEnumerable<byte[]> parts)
    {
        var list = parts as IList<byte[]> ?? parts.ToList();
        var total = list.Sum(p => p.Length);

        var res = new byte[total];
        var pos = 0;
        foreach (var p in list)
        {
            Buffer.BlockCopy(p, 0, res, pos, p.Length);
            pos += p.Length;
        }

        return res;
    }

    private sealed class ProfileContentAttrs
    {
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? TgUsername { get; init; }
        public string? Login { get; init; }
    }

    private static ProfileContentAttrs ExtractAttributes(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new ProfileContentAttrs();

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return new ProfileContentAttrs();

            return new ProfileContentAttrs
            {
                FirstName = GetValue("firstName"),
                LastName = GetValue("lastName"),
                TgUsername = GetValue("tgUsername"),
                Login = GetValue("login")
            };

            string? GetValue(string trait)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.ValueKind != JsonValueKind.Object) continue;
                    if (!el.TryGetProperty("trait_type", out var tt)) continue;
                    if (tt.ValueKind != JsonValueKind.String) continue;
                    if (!string.Equals(tt.GetString(), trait, StringComparison.Ordinal)) continue;

                    if (el.TryGetProperty("value", out var v))
                    {
                        return v.ValueKind switch
                        {
                            JsonValueKind.String => v.GetString(),
                            _ => v.ToString()
                        };
                    }
                }
                return null;
            }
        }
        catch
        {
            return new ProfileContentAttrs();
        }
    }

    private static string? ToLower(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var t = value.Trim();
        return t.Length > 0 ? t.ToLowerInvariant() : null;
    }

    private static string? SanitizeLogin(string? value)
    {
        if (value == null) return null;

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            var code = (int)ch;
            if (code is >= 32 and <= 126)
                sb.Append(ch);
        }

        var cleaned = sb.ToString().Trim().ToLowerInvariant();
        return cleaned.Length > 0 ? cleaned : null;
    }

    private static string? Capitalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length switch
        {
            0 => null,
            1 => trimmed.ToUpperInvariant(),
            _ => char.ToUpperInvariant(trimmed[0]) + trimmed[1..].ToLowerInvariant()
        };
    }
}
