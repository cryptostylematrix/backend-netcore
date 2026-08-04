namespace Contracts.Infrastructure.Ton;

public sealed class ProcessorWalletOptions
{
    public const string SectionName = "ProcessorWallet";

    public string Mnemonic { get; init; } = string.Empty;
    public string TransferAmountTon { get; init; } = "0.5";
    public int SeqnoTimeoutSeconds { get; init; } = 30;
    public int PollIntervalMilliseconds { get; init; } = 1000;
    public int MaxRetries { get; init; } = 10;
    public int RetryDelayMilliseconds { get; init; } = 2000;
}
