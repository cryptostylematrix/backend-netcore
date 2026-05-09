using Marketing.Application.Features.Matrix;

namespace Marketing.Presentation.Endpoints.Matrix.GetTree;

public sealed class GetTreeEndpoint(ISender sender) : Endpoint<GetTreeRequest, TreeNodeResponse>
{
    public override void Configure()
    {
        Get("/api/marketing/{marketing_addr}/tree");
        Tags("Matrix");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Tree";
            s.Description = "Get Tree";
            s.ExampleRequest = new GetTreeRequest
            {
                MarketingAddr = "E...",
                ProfileAddr = "E...",
                PlaceAddr = "E...",
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new TreeFilledNodeResponse
            {
                Locked = false,
                CanLock  = false,
                IsLock  = false,
                ParentAddr = null,
                Pos  = 1,
                Children = [
                    new TreeFilledNodeResponse
                    {
                        Locked = true,
                        CanLock  = false,
                        IsLock  = true,
                        ParentAddr = "E...",
                        Pos  = 0,
                        Children = [
                            new TreeEmptyNodeResponse
                            {
                                Locked = true,
                                CanLock  = false,
                                IsLock  = false,
                                ParentAddr = "E...",
                                Pos  = 0,
                                Children = null,
                                
                                IsNextPos = false,
                                CanBuy = false
                            },
                    
                            new TreeEmptyNodeResponse
                            {
                                Locked = true,
                                CanLock  = false,
                                IsLock  = false,
                                ParentAddr = "E...",
                                Pos  = 1,
                                Children = null,
                                
                                IsNextPos = false,
                                CanBuy = false
                            }
                        ],
                
                        Addr = "E...",
                        PlaceNumber = 2,
                        Kind = 1,
                        CreatedAt = 234567,
                        ProfileLogin = "bob",
                        ProfileAddr = "E...",
                        Descendants = 0,
                        IsRoot = false,
                    },
                    
                    new TreeFilledNodeResponse
                    {
                        Locked = false,
                        CanLock  = true,
                        IsLock  = false,
                        ParentAddr = "E...",
                        Pos  = 1,
                        Children = [
                            new TreeEmptyNodeResponse
                            {
                                Locked = false,
                                CanLock  = true,
                                IsLock  = false,
                                ParentAddr = "E...",
                                Pos  = 0,
                                Children = null,
                                
                                IsNextPos = true,
                                CanBuy = true
                            },
                    
                            new TreeEmptyNodeResponse
                            {
                                Locked = false,
                                CanLock  = true,
                                IsLock  = false,
                                ParentAddr = "E...",
                                Pos  = 1,
                                Children = null,
                                
                                IsNextPos = false,
                                CanBuy = false
                            }
                        ],
                
                        Addr = "E...",
                        PlaceNumber = 3,
                        Kind = 1,
                        CreatedAt = 345678,
                        ProfileLogin = "bob",
                        ProfileAddr = "E...",
                        Descendants = 0,
                        IsRoot = false,
                    }
                ],
                
                Addr = "E...",
                PlaceNumber = 1,
                Kind = 0,
                CreatedAt = 123456,
                ProfileLogin = "bob",
                ProfileAddr = "E...",
                Descendants = 2,
                IsRoot = true,
                
            };
        });
    }

    public override async Task HandleAsync(GetTreeRequest request, CancellationToken ct)
    {
        var query = new GetTreeQuery(
            MarketingAddr: request.MarketingAddr,
            ProfileAddr: request.ProfileAddr, 
            PlaceAddr: request.PlaceAddr,
            FromPos: request.FromPos,
            ToPos: request.ToPos);
        
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