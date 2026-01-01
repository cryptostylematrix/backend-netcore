namespace Contracts.Infrastructure.NftContent;

public interface INftContent { }

public sealed record NftContentOffchain(string Uri) : INftContent;

public sealed record NftContentOnchain(Dictionary<string, string?> Data) : INftContent;
