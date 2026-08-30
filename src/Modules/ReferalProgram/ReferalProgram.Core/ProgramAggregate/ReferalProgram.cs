using Common.Domain;

namespace ReferalProgram.Core.ProgramAggregate;

public sealed class ReferalProgram : Entity, IAggregateRoot
{
    private ReferalProgram()
    {
    }

    private ReferalProgram(string marketingAddr)
    {
        MarketingAddr = marketingAddr;
        IsTaskProcessingEnabled = true;
    }

    public string MarketingAddr { get; private set; } = null!;

    public bool IsTaskProcessingEnabled { get; private set; }

    public static ReferalProgram Create(string marketingAddr)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(marketingAddr);
        return new ReferalProgram(marketingAddr);
    }

    public void EnableTaskProcessing() => IsTaskProcessingEnabled = true;

    public void DisableTaskProcessing() => IsTaskProcessingEnabled = false;
}
