namespace Contracts.Dto;

public sealed class MarketingV3MessageBodyResponse
{
    [JsonPropertyName("boc_hex")]
    public string BocHex { get; init; } = null!;
}

public sealed class MarketingV3BasicDataResponse
{
    [JsonPropertyName("init")]
    public int Init { get; init; }

    [JsonPropertyName("admin_addr")]
    public string AdminAddr { get; init; } = null!;

    [JsonPropertyName("index")]
    public uint Index { get; init; }

    [JsonPropertyName("series_tag")]
    public uint SeriesTag { get; init; }

    [JsonPropertyName("metadata_uri")]
    public string? MetadataUri { get; init; }
}

public sealed class MarketingV3PlaceRef
{
    [JsonPropertyName("struct")]
    public byte Struct { get; init; }

    [JsonPropertyName("profile_addr")]
    public string? ProfileAddr { get; init; }

    [JsonPropertyName("place_number")]
    public uint PlaceNumber { get; init; }
}

public sealed class MarketingV3RelativePlaceRef
{
    [JsonPropertyName("source")]
    public MarketingV3PlaceRef Source { get; init; } = null!;

    [JsonPropertyName("level")]
    public ushort Level { get; init; }
}

public sealed class MarketingV3PlaceInfo
{
    [JsonPropertyName("place_number")]
    public uint PlaceNumber { get; init; }

    [JsonPropertyName("profile_login")]
    public string? ProfileLogin { get; init; }
}

public sealed class MarketingV3SourcePlace
{
    [JsonPropertyName("place")]
    public MarketingV3PlaceRef Place { get; init; } = null!;

    [JsonPropertyName("profile_login")]
    public string? ProfileLogin { get; init; }
}

public sealed class MarketingV3ProfileData
{
    [JsonPropertyName("profile_addr")]
    public string? ProfileAddr { get; init; }

    [JsonPropertyName("profile_login")]
    public string ProfileLogin { get; init; } = null!;

    [JsonPropertyName("owner_addr")]
    public string? OwnerAddr { get; init; }
}

public sealed class MarketingV3ProfileInfo
{
    [JsonPropertyName("profile_login")]
    public string ProfileLogin { get; init; } = null!;

    [JsonPropertyName("owner_addr")]
    public string? OwnerAddr { get; init; }
}

public sealed class MarketingV3FirstTaskResponse
{
    [JsonPropertyName("key")]
    public uint? Key { get; init; }

    [JsonPropertyName("val")]
    public MarketingV3TaskResponse? Val { get; init; }

    [JsonPropertyName("flag")]
    public int Flag { get; init; }
}

public sealed class MarketingV3TaskResponse
{
    [JsonPropertyName("query_id")]
    public ulong QueryId { get; init; }

    [JsonPropertyName("command")]
    public MarketingV3TaskCommandResponse? Command { get; init; }

    [JsonPropertyName("query")]
    public MarketingV3TaskQueryResponse? Query { get; init; }

    [JsonPropertyName("payload_boc_hex")]
    public string? PayloadBocHex { get; init; }
}

public sealed class MarketingV3TaskCommandResponse
{
    [JsonPropertyName("tag")]
    public uint Tag { get; init; }

    [JsonPropertyName("struct")]
    public byte? Struct { get; init; }

    [JsonPropertyName("command_struct")]
    public byte? CommandStruct { get; init; }

    [JsonPropertyName("command_tag")]
    public uint CommandTag { get; init; }

    [JsonPropertyName("profile_addr")]
    public string? ProfileAddr { get; init; }

    [JsonPropertyName("source_addr")]
    public string? SourceAddr { get; init; }

    [JsonPropertyName("amount")]
    public ulong? Amount { get; init; }

    [JsonPropertyName("sender_jetton_wallet")]
    public string? SenderJettonWallet { get; init; }

    [JsonPropertyName("relative")]
    public MarketingV3RelativePlaceRef? Relative { get; init; }
}

public sealed class MarketingV3TaskQueryResponse
{
    [JsonPropertyName("tag")]
    public uint Tag { get; init; }

    [JsonPropertyName("struct")]
    public byte? Struct { get; init; }

    [JsonPropertyName("bonus_type_tag")]
    public uint BonusTypeTag { get; init; }

    [JsonPropertyName("relative")]
    public MarketingV3RelativePlaceRef? Relative { get; init; }

    [JsonPropertyName("reason")]
    public MarketingV3PlaceInfo? Reason { get; init; }

    [JsonPropertyName("recipient_profile_addr")]
    public string? RecipientProfileAddr { get; init; }

    [JsonPropertyName("amount")]
    public ulong Amount { get; init; }

    [JsonPropertyName("sender_jetton_wallet")]
    public string? SenderJettonWallet { get; init; }

    [JsonPropertyName("bonus_title")]
    public string BonusTitle { get; init; } = null!;
}

public sealed class MarketingV3DataResponse
{
    [JsonPropertyName("admin_addr")]
    public string AdminAddr { get; init; } = null!;

    [JsonPropertyName("index")]
    public uint Index { get; init; }

    [JsonPropertyName("series_tag")]
    public uint SeriesTag { get; init; }

    [JsonPropertyName("metadata_uri")]
    public string MetadataUri { get; init; } = null!;

    [JsonPropertyName("max_tasks")]
    public ushort MaxTasks { get; init; }

    [JsonPropertyName("queue_size")]
    public ushort QueueSize { get; init; }

    [JsonPropertyName("seq_no")]
    public uint SeqNo { get; init; }

    [JsonPropertyName("processor_addr")]
    public string ProcessorAddr { get; init; } = null!;

    [JsonPropertyName("queue")]
    public IDictionary<uint, MarketingV3TaskResponse> Queue { get; init; }
        = new Dictionary<uint, MarketingV3TaskResponse>();

    [JsonPropertyName("structures")]
    public IDictionary<byte, MarketingV3StructureConfigResponse> Structures { get; init; }
        = new Dictionary<byte, MarketingV3StructureConfigResponse>();

    [JsonPropertyName("prefix_boc_hex")]
    public string PrefixBocHex { get; init; } = null!;
}

public sealed class MarketingV3StructureConfigResponse
{
    [JsonPropertyName("commands")]
    public IDictionary<uint, MarketingV3CommandConfigResponse> Commands { get; init; }
        = new Dictionary<uint, MarketingV3CommandConfigResponse>();

    [JsonPropertyName("rewards")]
    public IDictionary<uint, MarketingV3RewardConfigResponse> Rewards { get; init; }
        = new Dictionary<uint, MarketingV3RewardConfigResponse>();

    [JsonPropertyName("royalties")]
    public IDictionary<string, MarketingV3RoyaltyConfigResponse> Royalties { get; init; }
        = new Dictionary<string, MarketingV3RoyaltyConfigResponse>();

    [JsonPropertyName("name")]
    public string Name { get; init; } = null!;
}

public sealed class MarketingV3CommandConfigResponse
{
    [JsonPropertyName("price")]
    public ulong Price { get; init; }

    [JsonPropertyName("sender_jetton_wallet")]
    public string? SenderJettonWallet { get; init; }

    [JsonPropertyName("gram_fee")]
    public ulong GramFee { get; init; }
}

public sealed class MarketingV3RewardConfigResponse
{
    [JsonPropertyName("sets")]
    public IDictionary<byte, IReadOnlyCollection<MarketingV3RewardResponse>> Sets { get; init; }
        = new Dictionary<byte, IReadOnlyCollection<MarketingV3RewardResponse>>();
}

public sealed class MarketingV3RoyaltyConfigResponse
{
    [JsonPropertyName("numerator")]
    public ushort Numerator { get; init; }

    [JsonPropertyName("denominator")]
    public ushort Denominator { get; init; }

    [JsonPropertyName("recipient")]
    public string? Recipient { get; init; }
}

public sealed class MarketingV3RewardResponse
{
    [JsonPropertyName("tag")]
    public uint Tag { get; init; }

    [JsonPropertyName("from_level")]
    public ushort? FromLevel { get; init; }

    [JsonPropertyName("to_level")]
    public ushort? ToLevel { get; init; }

    [JsonPropertyName("count")]
    public byte? Count { get; init; }

    [JsonPropertyName("struct")]
    public byte? Struct { get; init; }

    [JsonPropertyName("command_struct")]
    public byte? CommandStruct { get; init; }

    [JsonPropertyName("command_tag")]
    public uint? CommandTag { get; init; }

    [JsonPropertyName("bonus_type_tag")]
    public uint? BonusTypeTag { get; init; }

    [JsonPropertyName("profile_addr")]
    public string? ProfileAddr { get; init; }

    [JsonPropertyName("recipient")]
    public string? Recipient { get; init; }

    [JsonPropertyName("amount")]
    public ulong? Amount { get; init; }

    [JsonPropertyName("sender_jetton_wallet")]
    public string? SenderJettonWallet { get; init; }

    [JsonPropertyName("forward_ton_amount")]
    public ulong? ForwardTonAmount { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("payload_boc_hex")]
    public string? PayloadBocHex { get; init; }
}
