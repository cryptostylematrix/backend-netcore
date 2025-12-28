using Contracts.Application.Features.Multi;

namespace Contracts.Presentation.Endpoints.Multi.GetMultiData;

public sealed class GetMultiDataEndpoint(ISender sender) : EndpointWithoutRequest<MultiDataResponse>
{
    public override void Configure()
    {
        Get("contracts/multi/data");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Multi Data";
            s.Description = "Get Multi Data";
            s.ResponseExamples[StatusCodes.Status200OK] = new MultiDataResponse
            {
                ProcessorAddr = "E...",
                MaxTasks = 200,
                QueueSize = 10,
                SeqNo = 1234,
                Fees = new MultiFeesDataResponse
                {
                    M1 = 5_000_000,
                    M2 = 5_000_000,
                    M3 = 5_000_000,
                    M4 = 10_000_000,
                    M5 = 10_000_000,
                    M6 = 10_000_000,
                },
                Security = new MultiSecurityDataResponse
                {
                    AdminAddr = "E..."
                },
                PlaceCode = "tAd...",
                Tasks =
                [
                    new MultiQueueItemResponse
                    {
                        Key = 11,
                        Val = new MultiTaskItemResponse
                        {
                            M = 1,
                            ProfileAddr = "E...",
                            QueryId = 345,
                            Payload = new MultiTaskPayloadResponse
                            {
                                Tag = 1,
                                SourceAddr = "E...", 
                                Pos = new PlacePosDataResponse
                                {
                                    ParentAddr = "E...",
                                    Pos = 1
                                }
                            }
                        }
                    }
                ]
            };
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var query = new GetMultiDataQuery();
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