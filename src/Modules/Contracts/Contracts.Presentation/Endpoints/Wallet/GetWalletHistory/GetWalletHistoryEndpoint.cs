using Contracts.Application.Features.Wallet;

namespace Contracts.Presentation.Endpoints.Wallet.GetWalletHistory;


public sealed class GetWalletHistoryEndpoint(ISender sender) : Endpoint<GetWalletHistoryRequest, TransactionHistoryResponse>
{
    public override void Configure()
    {
        Get("contracts/wallet/{addr}/history");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Wallet's Transaction history";
            s.Description = "Get Wallet's Transaction history";
            s.ExampleRequest = new GetWalletHistoryRequest
            {
                Addr = "E...",
                Limit = 20,
                Lt = 123,
                Hash = "abc...",
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new TransactionHistoryResponse
            {
                Items =
                [
                    new TransactionResponse
                    {
                        Lt = 123,
                        Hash = "abc...",
                        UTime = 123,
                        Messages = [
                            new TransactionMessageResponse
                            {
                                Addr = "E...",
                                Comment = "comment",
                                Op = "op",
                                ProfileAddr = "E...",
                                Value = 123
                            }
                        ]
                    }
                ]
            };
        });
    }

    public override async Task HandleAsync(GetWalletHistoryRequest request, CancellationToken ct)
    {
        var query = new GetWalletHistoryQuery(
            Addr: request.Addr,
            Limit: request.Limit,
            Lt: request.Lt,
            Hash: request.Hash);
        
        var result = await sender.Send(query, ct);
        
        if (!result.IsSuccess)
        {
            await Send.ResultAsync(result.ToResult());
        }
        else
        {
            Response = result.Value;
        }
    }
}