using Contracts.Application.Features.Marketing;

namespace Contracts.Presentation.Endpoints.Marketing.GetMarketingData;


public sealed class GetMarketingDataEndpoint(ISender sender) : 
    Endpoint<GetMarketingDataRequest, MarketingDataResponse>
{
    public override void Configure()
    {
        Get("contracts/marketing/{addr}/data");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Marketing Data";
            s.Description = "Get Marketing Data";
        //     s.ResponseExamples[StatusCodes.Status200OK] = new MarketingDataResponse
        //     {
        //         AdminAddr = "E...",
        //         Index = 123,
        //         MaxTasks = 200,
        //         QueueSize = 10,
        //         SeqNo = 1234,
        //         ProcessorAddr = "E...",
        //         JettonWalletAddr= "E...",
        //         InitialFee = 5m,
        //         Queue = {
        //             { 1, new MarketingTaskResponse {
        //                 QueryId = 123,
        //                 M = 2,
        //                 ProfileAddr =  "E...",
        //                 Payload = new MarketingTaskPayloadResponse
        //                 {
        //                     Tag = 2
        //                 }
        //             }},
        //             { 2, new MarketingTaskResponse {
        //                 QueryId = 123,
        //                 M = 2,
        //                 ProfileAddr =  "E...",
        //                 Payload = new MarketingTaskPayloadResponse
        //                 {
        //                     Tag = 6
        //                 }
        //             }},
        //         },
        //         
        //         Matrixes =
        //         {
        //             { 1, new MatrixConfigResponse {
        //                 Price = 10m,
        //                 OwnerAddr = "E...",
        //                 RoyaltyNumerator = 10,
        //                 RoyaltyDenominator = 100,
        //                 Width = 0,
        //                 Height = 1,
        //                 Code = "bcd..",
        //                 Rewards =
        //                 {
        //                     { 1, [] },
        //                     { 2, [] },
        //                 },
        //                 Name = "Matrix 1"
        //             }},
        //             { 2, new MatrixConfigResponse {
        //                 Price = 20m,
        //                 OwnerAddr = "E...",
        //                 RoyaltyNumerator = 10,
        //                 RoyaltyDenominator = 100,
        //                 Width = 4,
        //                 Height = 1,
        //                 Code = "bcd..",
        //                 Rewards =
        //                 {
        //                     { 1, [] },
        //                     { 2, [] },
        //                 },
        //                 Name = "Matrix 2"
        //             }},
        //         },
        //         
        //         Fees =
        //         {
        //             { 1, 0.5m },
        //             { 2, 0.5m }
        //         },
        //         
        //         Params = new MarketingParamsResponse
        //         {
        //             
        //         }
        //     };
        });
    }

    public override async Task HandleAsync(GetMarketingDataRequest request, CancellationToken ct)
    {
        var query = new GetMarketingDataQuery(request.Addr);
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