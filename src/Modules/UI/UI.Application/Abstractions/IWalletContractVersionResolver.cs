namespace UI.Application.Abstractions;

public interface IWalletContractVersionResolver
{
    // False means malformed StateInit or StateInit belonging to a different address.
    bool TryResolve(string walletAddr, string? stateInit, out string version);
}
