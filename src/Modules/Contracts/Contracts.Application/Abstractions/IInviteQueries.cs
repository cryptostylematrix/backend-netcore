namespace Contracts.Application.Abstractions;

public interface IInviteQueries
{
    Task<Result<InviteAddressResponse>> GetInviteAddressBySeqNoAsync(
        string addr,
        uint seqNo,
        CancellationToken ct = default);

    Task<Result<InviteDataResponse>> GetInviteDataAsync(
        string addr,
        CancellationToken ct = default);
}