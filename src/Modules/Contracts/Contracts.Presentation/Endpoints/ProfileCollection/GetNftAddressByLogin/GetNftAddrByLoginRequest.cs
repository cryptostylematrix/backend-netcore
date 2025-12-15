namespace Contracts.Presentation.Endpoints.ProfileCollection.GetNftAddressByLogin;

public sealed class GetNftAddrByLoginRequest
{
    public string Login { get; init; } = null!;
}