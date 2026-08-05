namespace ReferalProgram.Dto;

public sealed record CommandResponse(
    uint Code,
    PlaceResponse Source);
