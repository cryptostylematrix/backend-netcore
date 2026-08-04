namespace Contracts.Application.Abstractions;

public interface IMarketingTransactionSender
{
    Task SendAsync(
        string marketingAddr,
        uint taskKey,
        string bodyBocHex,
        CancellationToken cancellationToken);
}
