using System.Text.Json;
using Contracts.Application.Features.ProfileCollection;
using Contracts.Application.Features.ProfileItem;
using MediatR;
using UI.Application.Abstractions;

namespace UI.Application.Services;

internal sealed class ProfileContractReader(ISender sender) : IProfileContractReader
{
    public async Task<ProfileContractLookup> GetByLoginAsync(
        string login,
        CancellationToken cancellationToken)
    {
        var normalizedLogin = login.Trim().ToLowerInvariant();
        var addressResult = await sender.Send(
            new GetNftAddressByLoginQuery(normalizedLogin),
            cancellationToken);

        if (!addressResult.IsSuccess)
            return FailureFrom(addressResult.Errors);

        var profileResult = await sender.Send(
            new GetFreshNftDataQuery(addressResult.Value.Addr),
            cancellationToken);

        if (!profileResult.IsSuccess)
            return FailureFrom(profileResult.Errors);

        var profile = profileResult.Value;
        if (profile.Content is null
            || string.IsNullOrWhiteSpace(profile.Content.Login))
        {
            return ProfileContractLookup.Failure(UiErrorCodes.ProfileNotFound);
        }

        return ProfileContractLookup.Success(new ProfileContractSnapshot(
            addressResult.Value.Addr,
            profile.Content.Login.Trim().ToLowerInvariant(),
            profile.OwnerAddr,
            JsonSerializer.Serialize(profile.Content)));
    }

    private static ProfileContractLookup FailureFrom(
        IEnumerable<string> contractErrors)
    {
        var errors = contractErrors.ToArray();
        var notFound = errors.Any(error =>
            error.Contains("GetMethodFailed", StringComparison.Ordinal)
            || error.Contains("not found", StringComparison.OrdinalIgnoreCase));

        return ProfileContractLookup.Failure(notFound
            ? UiErrorCodes.ProfileNotFound
            : UiErrorCodes.ContractRequestFailed);
    }
}
