namespace ReferalProgram.Dto;

public sealed record BonusResponse(
    PlaceResponse Reason,
    string RecipientProfileAddr);
