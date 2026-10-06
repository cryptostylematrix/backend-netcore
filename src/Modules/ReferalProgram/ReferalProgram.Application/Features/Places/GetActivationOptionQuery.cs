using System.Text.Json;
using ReferalProgram.Application.Services;

namespace ReferalProgram.Application.Features.Places;

public sealed record GetActivationOptionQuery(string MarketingAddr, byte StructureNumber,
    string ProfileAddr, uint PlaceNumber) : IQuery<ActivationOptionResponse>;

internal sealed class GetActivationOptionQueryHandler(IStructureQueries structures,
    IPlaceQueries places, IActivatePlacePolicy activationPolicy)
    : IQueryHandler<GetActivationOptionQuery, ActivationOptionResponse>
{
    public async Task<Result<ActivationOptionResponse>> Handle(GetActivationOptionQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.MarketingAddr) || string.IsNullOrWhiteSpace(request.ProfileAddr)
            || request.PlaceNumber == 0)
            return Result<ActivationOptionResponse>.Error("Marketing, profile and place are required.");

        var structure = await structures.GetStructureAsync(request.MarketingAddr, request.StructureNumber, ct);
        if (structure is null) return Denied("structure_not_found");
        if (structure.Activity is null) return Denied("activity_configuration_missing");
        byte? source;
        try
        {
            ActivitySettings.Parse(structure.Activity.Value, structure.StructureNumber);
            source = await ActivitySourceResolver.ResolveAsync(structure, places, ct);
        }
        catch (JsonException) { return Denied("activity_configuration_invalid"); }

        // A displayed place must exist; a missing source never falls back to that place.
        if (source is not null && await places.GetPlaceAsync(request.MarketingAddr,
                request.StructureNumber, request.ProfileAddr, request.PlaceNumber, ct) is null)
            return Denied("place_not_found");

        var targetStructure = source ?? request.StructureNumber;
        var targetPlace = source is null ? request.PlaceNumber : 1u;
        var decision = await activationPolicy.EvaluateAsync(request.MarketingAddr,
            targetStructure, request.ProfileAddr, targetPlace, ct);
        return Result.Success(new ActivationOptionResponse
        {
            CanActivate = decision.CanActivate, CommandTag = decision.CommandTag,
            StructureNumber = targetStructure, ProfileAddr = request.ProfileAddr,
            PlaceNumber = targetPlace, Reason = decision.Reason
        });
    }

    private static Result<ActivationOptionResponse> Denied(string reason) =>
        Result.Success(new ActivationOptionResponse { Reason = reason });
}
