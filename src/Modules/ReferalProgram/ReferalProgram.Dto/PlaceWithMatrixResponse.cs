namespace ReferalProgram.Dto;

public sealed class PlaceWithMatrixResponse : PlaceResponse
{
    [JsonPropertyName("matrix_size")]
    public long MatrixSize { get; init; }

    [JsonPropertyName("matrix_filling")]
    public long MatrixFilling { get; init; }
}
