using System.Text.RegularExpressions;
using UI.Application.Abstractions;
using UI.Core.WalletPreferencesAggregate;
using WalletPreferencesEntity = UI.Core.WalletPreferencesAggregate.WalletPreferences;

namespace UI.Application.Features.WalletPreferences;

public sealed record SaveWalletLanguageCommand(string WalletAddr, string? Language,
    bool Overwrite) : ICommand<WalletLanguageResponse>;

internal sealed class SaveWalletLanguageCommandHandler(
    IWalletAddressService addresses,
    IWalletPreferencesRepository repository,
    TimeProvider timeProvider) : ICommandHandler<SaveWalletLanguageCommand, WalletLanguageResponse>
{
    // Validate a bounded language tag, independently of the frontend translation catalog.
    private static readonly Regex LanguageTag = new(
        @"\A[a-z]{2,8}(?:-[a-z0-9]{1,8})*\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public async Task<Result<WalletLanguageResponse>> Handle(
        SaveWalletLanguageCommand request, CancellationToken cancellationToken)
    {
        if (!addresses.TryNormalize(request.WalletAddr, out var walletAddr))
            return Failure(UiErrorCodes.InvalidWalletAddress);
        var language = request.Language?.Trim().ToLowerInvariant();
        if (language is null || language.Length > 63 || !LanguageTag.IsMatch(language))
            return Failure(UiErrorCodes.InvalidLanguage);

        var stored = await repository.SaveLanguageAsync(
            WalletPreferencesEntity.Create(walletAddr, language, timeProvider.GetUtcNow().UtcDateTime),
            request.Overwrite, cancellationToken);
        return Result.Success(new WalletLanguageResponse { Success = true, Language = stored });
    }

    private static Result<WalletLanguageResponse> Failure(string error) =>
        Result.Success(new WalletLanguageResponse { Errors = [error] });
}
