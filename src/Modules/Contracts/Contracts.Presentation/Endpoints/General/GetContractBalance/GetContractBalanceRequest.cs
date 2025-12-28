namespace Contracts.Presentation.Endpoints.General.GetContractBalance;

public sealed class GetContractBalanceRequest
{
    public string Addr { get; init; } = null!;
}