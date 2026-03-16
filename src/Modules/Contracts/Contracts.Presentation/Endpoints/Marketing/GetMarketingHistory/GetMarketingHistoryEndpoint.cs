using Contracts.Application.Features.Marketing;

namespace Contracts.Presentation.Endpoints.Marketing.GetMarketingHistory;

public sealed class GetMarketingHistoryEndpoint(ISender sender) : Endpoint<GetMarketingHistoryRequest, MarketingTransactionHistoryResponse>
{
    public override void Configure()
    {
        Get("contracts/marketing/{addr}/history");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Marketing's Transaction history";
            s.Description = "Get Marketing's Transaction history";
            s.ExampleRequest = new GetMarketingHistoryRequest
            {
                Addr = "E...",
                Limit = 20,
                Lt = 123,
                Hash = "abc...",
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new MarketingTransactionHistoryResponse
            {
                Items =
                [
                    new MarketingTransactionResponse
                    {
                        Lt = 123,
                        Hash = "abc...",
                        UTime = 123,
                        Messages = [
                            new MarketingTransactionMessageResponse
                            {
                                FromAddr = "E...",
                                ToAddr = "E...",
                                Comment = "comment",
                                Op = "op",
                                ProfileAddr = "E...",
                                Value = 123,
                                Key = 123,
                                QueryId = 123,
                                M = 2,
                                ParentAddr = "E...",
                                Pos = 0
                            }
                        ]
                    }
                ]
            };
        });
    }

    public override async Task HandleAsync(GetMarketingHistoryRequest request, CancellationToken ct)
    {
        var query = new GetMarketingHistoryQuery(
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