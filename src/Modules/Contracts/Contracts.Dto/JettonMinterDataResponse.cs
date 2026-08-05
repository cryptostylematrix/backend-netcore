namespace Contracts.Dto;

public sealed class JettonMinterDataResponse
{
    [JsonPropertyName("total_supply")]
    public string TotalSupply { get; init; } = null!;

    [JsonPropertyName("mintable")]
    public bool Mintable { get; init; }

    [JsonPropertyName("admin_address")]
    public string AdminAddress { get; init; } = null!;

    [JsonPropertyName("metadata_uri")]
    public string? MetadataUri { get; init; }

    [JsonPropertyName("decimals")]
    public byte? Decimals { get; init; }

    [JsonPropertyName("content_boc_hex")]
    public string ContentBocHex { get; init; } = null!;

    [JsonPropertyName("wallet_code_boc_hex")]
    public string WalletCodeBocHex { get; init; } = null!;
}
