namespace UI.Dto;

public sealed class UiReportResponse
{
    [JsonPropertyName("generated_at")] public DateTime GeneratedAt { get; init; }
    [JsonPropertyName("profiles")] public ProfileReport Profiles { get; init; } = new();
    [JsonPropertyName("ton_connect")] public ConnectionReport TonConnect { get; init; } = new();
    [JsonPropertyName("activity")] public ActivityReport Activity { get; init; } = new();
    [JsonPropertyName("preferences")] public PreferenceReport Preferences { get; init; } = new();
}
public sealed class ProfileReport
{
    [JsonPropertyName("total_wallets")] public long TotalWallets { get; init; }
    [JsonPropertyName("total_profiles")] public long TotalProfiles { get; init; }
    [JsonPropertyName("page")] public int Page { get; init; }
    [JsonPropertyName("page_size")] public int PageSize => 10;
    [JsonPropertyName("items")] public IReadOnlyList<ProfileReportRow> Items { get; init; } = [];
}
public sealed class ProfileReportRow
{
    [JsonPropertyName("wallet_addr")] public string WalletAddr { get; init; } = "";
    [JsonPropertyName("profile_count")] public long ProfileCount { get; init; }
    [JsonPropertyName("percentage")] public decimal Percentage { get; init; }
}
public sealed class ConnectionReport
{
    [JsonPropertyName("total")] public long Total { get; init; }
    [JsonPropertyName("groups")] public IReadOnlyList<ConnectionReportGroup> Groups { get; init; } = [];
}
public sealed class ConnectionReportGroup
{
    [JsonPropertyName("contract_version")] public string? ContractVersion { get; init; }
    [JsonPropertyName("wallet_name")] public string? WalletName { get; init; }
    [JsonPropertyName("app_version")] public string? AppVersion { get; init; }
    [JsonPropertyName("platform")] public string? Platform { get; init; }
    [JsonPropertyName("count")] public long Count { get; init; }
    [JsonPropertyName("percentage")] public decimal Percentage { get; init; }
}
public sealed class ActivityReport
{
    [JsonPropertyName("from")] public DateTime From { get; init; }
    [JsonPropertyName("to")] public DateTime To { get; init; }
    [JsonPropertyName("total")] public long Total { get; init; }
    [JsonPropertyName("page")] public int Page { get; init; }
    [JsonPropertyName("page_size")] public int PageSize => 10;
    [JsonPropertyName("items")] public IReadOnlyList<ActivityReportRow> Items { get; init; } = [];
}
public sealed class ActivityReportRow
{
    [JsonPropertyName("wallet_addr")] public string WalletAddr { get; init; } = "";
    [JsonPropertyName("last_connected_at")] public DateTime LastConnectedAt { get; init; }
}
public sealed class PreferenceReport
{
    [JsonPropertyName("total")] public long Total { get; init; }
    [JsonPropertyName("groups")] public IReadOnlyList<PreferenceReportGroup> Groups { get; init; } = [];
}
public sealed class PreferenceReportGroup
{
    [JsonPropertyName("language")] public string Language { get; init; } = "";
    [JsonPropertyName("count")] public long Count { get; init; }
    [JsonPropertyName("percentage")] public decimal Percentage { get; init; }
}

public sealed class UiReportSectionResponse<T>
{
    [JsonPropertyName("generated_at")] public DateTime GeneratedAt { get; init; }
    [JsonPropertyName("data")] public required T Data { get; init; }
}
