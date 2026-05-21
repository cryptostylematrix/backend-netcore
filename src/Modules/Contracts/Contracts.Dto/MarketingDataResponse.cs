namespace Contracts.Dto;

public sealed class MarketingDataResponse
{
    [JsonPropertyName("admin_addr")]
    public string AdminAddr { get; init; } = null!;
    
    [JsonPropertyName("index")]
    public uint Index  { get; init; }
    
    [JsonPropertyName("max_tasks")]
    public uint MaxTasks { get; init; }
    
    [JsonPropertyName("queue_size")]
    public uint QueueSize  { get; init; }
    
    [JsonPropertyName("seq_no")]
    public uint SeqNo { get; init; }
    
    [JsonPropertyName("processor_addr")]
    public string ProcessorAddr { get; init; } = null!;
    
    [JsonPropertyName("jetton_wallet_addr")]
    public string? JettonWalletAddr { get; init; }
    
    [JsonPropertyName("initial_fee")]
    public ulong InitialFee { get; init; }
    
    [JsonPropertyName("queue")]
    public Dictionary<uint, MarketingTaskResponse> Queue { get; init; } = null!;
    
    [JsonPropertyName("matrixes")] 
    public IDictionary<byte, MatrixConfigResponse> Matrixes { get; init; } = null!;
    
    [JsonPropertyName("fees")] 
    public IDictionary<byte, decimal> Fees { get; init; } = null!;
    
    [JsonPropertyName("params")] 
    public MarketingParamsResponse Params { get; init; } = null!;
}

public sealed class MatrixConfigResponse
{
    [JsonPropertyName("price")] 
    public ulong Price { get; init; }

    [JsonPropertyName("owner_addr")] 
    public string OwnerAddr { get; init; } = null!;
    
    [JsonPropertyName("royalty_numerator")] 
    public ushort RoyaltyNumerator { get; init; }
    
    [JsonPropertyName("royalty_denominator")] 
    public ushort RoyaltyDenominator { get; init; }
    
    [JsonPropertyName("width")] 
    public byte Width { get; init; }
    
    [JsonPropertyName("height")] 
    public byte Height { get; init; }

    [JsonIgnore] 
    public string Code { get; init; } = null!;

    [JsonPropertyName("rewards")] 
    public IDictionary<byte, IEnumerable<RewardResponse>> Rewards { get; init; } = null!;
 
    [JsonPropertyName("name")] 
    public string Name { get; init; } = null!;
}

public sealed class MarketingParamsResponse
{
    [JsonPropertyName("version")]
    public uint? Version { get; init; }
    
    [JsonPropertyName("program_id")]
    public uint? ProgramId { get; init; }
    
    [JsonPropertyName("metadata_uri")]
    public string? MetadataUri { get; init; }
    
    [JsonPropertyName("program_features")]
    public ProgramFeaturesResponse? ProgramFeatures { get; init; }
    
    [JsonPropertyName("matrix_features")] 
    public IDictionary<byte, MatrixFeaturesResponse>? MatrixFeatures { get; init; }
}

public sealed class ProgramFeaturesResponse
{
    [JsonPropertyName("version")]
    public uint? Version { get; init; }
    
    [JsonPropertyName("admin_locks")]
    public bool? AdminLocks { get; init; }
    
    [JsonPropertyName("subscription")]
    public ProgramSubscriptionResponse? Subscription { get; init; }
}

public sealed class ProgramSubscriptionResponse
{
    
}
public sealed class MatrixFeaturesResponse
{
    [JsonPropertyName("version")]
    public uint? Version { get; init; }
    
    [JsonPropertyName("distribution")]
    public string? Distribution { get; init; }
    
    [JsonPropertyName("management")]
    public string? Management { get; init; }
  
    [JsonPropertyName("cut_factor")]
    public byte? CutFactor { get; init; }
    
    [JsonPropertyName("prev_required")]
    public bool? PrevRequired { get; init; }
}


public sealed class RewardResponse
{
    [JsonPropertyName("tag")]
    public string Tag { get; init; } = null!;
    
    [JsonPropertyName("m")]
    public byte? M { get; init; }
    
    [JsonPropertyName("count")]
    public byte? Count { get; init; }
    
    [JsonPropertyName("amount")]
    public ulong? Amount { get; init; }
}
