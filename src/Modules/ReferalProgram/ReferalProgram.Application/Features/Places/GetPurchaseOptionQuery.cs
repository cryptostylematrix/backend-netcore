namespace ReferalProgram.Application.Features.Places;

public sealed record GetPurchaseOptionQuery(
    string MarketingAddr,
    byte StructureNumber,
    string ProfileAddr,
    string? ParentProfileAddr,
    uint? ParentPlaceNumber,
    uint? Position) : IQuery<PurchaseOptionResponse>;

internal sealed class GetPurchaseOptionQueryHandler(IBuyPlacePolicy buyPlacePolicy)
    : IQueryHandler<GetPurchaseOptionQuery, PurchaseOptionResponse>
{
    public async Task<Result<PurchaseOptionResponse>> Handle(
        GetPurchaseOptionQuery request,
        CancellationToken cancellationToken)
    {
        var hasAnyPositionPart = request.ParentProfileAddr is not null
            || request.ParentPlaceNumber is not null
            || request.Position is not null;
        var hasRequiredPositionParts = request.ParentPlaceNumber is not null
            && request.Position is not null;

        if (hasAnyPositionPart && !hasRequiredPositionParts)
            return Result<PurchaseOptionResponse>.Error(
                "ParentPlaceNumber and Position must be provided together.");

        var requestedPosition = hasRequiredPositionParts
            ? new RequestedPosition(
                request.StructureNumber,
                request.ParentProfileAddr,
                request.ParentPlaceNumber!.Value,
                request.Position!.Value)
            : null;

        var decision = await buyPlacePolicy.EvaluateAsync(
            request.MarketingAddr,
            request.StructureNumber,
            request.ProfileAddr,
            requestedPosition,
            cancellationToken);

        return Result.Success(new PurchaseOptionResponse
        {
            CanBuy = decision.CanBuy,
            CommandTag = decision.CommandTag,
            IncludePosition = decision.IncludePosition,
            Position = decision.Position,
            Reason = decision.Reason
        });
    }
}
