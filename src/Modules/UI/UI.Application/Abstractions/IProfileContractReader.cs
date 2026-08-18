namespace UI.Application.Abstractions;

public interface IProfileContractReader
{
    Task<ProfileContractLookup> GetByLoginAsync(
        string login,
        CancellationToken cancellationToken);
}

public sealed record ProfileContractLookup(
    ProfileContractSnapshot? Profile,
    string? ErrorCode)
{
    public bool IsSuccess => Profile is not null;

    public static ProfileContractLookup Success(ProfileContractSnapshot profile) =>
        new(profile, null);

    public static ProfileContractLookup Failure(string errorCode) =>
        new(null, errorCode);
}

public sealed record ProfileContractSnapshot(
    string Address,
    string Login,
    string? OwnerAddr,
    string ContentJson);
