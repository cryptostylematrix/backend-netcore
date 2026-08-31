using Contracts.Application.Abstractions;
using Microsoft.Extensions.Options;
using TonSdk.Contracts.Wallet;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;
using TonSdk.Core.Crypto;

namespace Contracts.Infrastructure.Ton;

public sealed class MarketingTransactionSender : IMarketingTransactionSender
{
    private const byte SendMode = 3;

    private readonly ITonClient _tonClient;
    private readonly ProcessorWalletOptions _options;
    private readonly KeyPair _keys;
    private readonly WalletV4 _wallet;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public MarketingTransactionSender(
        ITonClient tonClient,
        IOptions<ProcessorWalletOptions> options)
    {
        _tonClient = tonClient;
        _options = options.Value;

        var words = _options.Mnemonic.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _keys = new Mnemonic(words).Keys;
        _wallet = new WalletV4(new WalletV4Options
        {
            Workchain = 0,
            PublicKey = _keys.PublicKey
        });
    }

    public async Task SendAsync(
        string marketingAddr,
        uint taskKey,
        string bodyBocHex,
        CancellationToken cancellationToken)
    {
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            var isDeployed = await RetryAsync(
                () => _tonClient.IsContractDeployed(_wallet.Address),
                cancellationToken);
            var seqno = isDeployed
                ? await RetryAsync(
                      async () => await _tonClient.Wallet.GetSeqno(_wallet.Address)
                          ?? throw new InvalidOperationException(
                              "Could not retrieve processor wallet seqno."),
                      cancellationToken)
                : 0;
            var body = Cell.From(new Bits(Convert.FromHexString(bodyBocHex)));
            var message = new InternalMessage(new InternalMessageOptions
            {
                Info = new IntMsgInfo(new IntMsgInfoOptions
                {
                    Bounce = true,
                    Dest = new Address(marketingAddr),
                    Value = new Coins(_options.TransferAmountTon)
                }),
                Body = body
            });
            var externalMessage = _wallet.CreateTransferMessage(
                [new WalletTransfer { Message = message, Mode = SendMode }],
                seqno);

            externalMessage.Sign(_keys.PrivateKey);
            await RetryAsync(
                async () => await _tonClient.SendBoc(externalMessage.Cell)
                    ?? throw new InvalidOperationException("TON API did not accept the wallet message."),
                cancellationToken);
            await WaitForSeqnoAsync(seqno, cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task WaitForSeqnoAsync(
        uint previousSeqno,
        CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(_options.SeqnoTimeoutSeconds);
        var startedAt = DateTime.UtcNow;

        while (DateTime.UtcNow - startedAt <= timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentSeqno = await RetryAsync(
                async () => await _tonClient.Wallet.GetSeqno(_wallet.Address)
                    ?? throw new InvalidOperationException(
                        "Could not retrieve processor wallet seqno."),
                cancellationToken);
            if (currentSeqno > previousSeqno)
                return;

            await Task.Delay(_options.PollIntervalMilliseconds, cancellationToken);
        }

        throw new TimeoutException("Timed out waiting for processor wallet seqno to advance.");
    }

    private async Task<T> RetryAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch when (attempt < _options.MaxRetries)
            {
                var delay = checked(
                    _options.RetryDelayMilliseconds * (long)Math.Pow(2, attempt));
                await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationToken);
            }
        }
    }
}
