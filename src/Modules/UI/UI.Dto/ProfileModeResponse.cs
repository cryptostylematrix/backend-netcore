namespace UI.Dto;

[JsonConverter(typeof(JsonStringEnumConverter<ProfileModeResponse>))]
public enum ProfileModeResponse
{
    [JsonStringEnumMemberName("owner")]
    Owner,

    [JsonStringEnumMemberName("preview")]
    Preview
}
