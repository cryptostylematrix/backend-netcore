using Matrix.Application.Features.Matrix;

namespace Matrix.Presentation.Endpoints.Matrix.GetTree;

public sealed class GetTreeEndpoint(ISender sender) : Endpoint<GetTreeRequest, TreeNodeResponse>
{
    public override void Configure()
    {
        Get("/api/matrix/{ProfileAddr}/tree/{PlaceAddr}");
        Tags("Matrix");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Tree";
            s.Description = "Get Tree";
            s.ExampleRequest = new GetTreeRequest
            {
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
                        Clone = 1,
                        CreatedAt = 234567,
                        Login = "bob",
                        ImageUrl = "https://example.com/avatar.png",
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
                        Clone = 1,
                        CreatedAt = 345678,
                        Login = "bob",
                        ImageUrl = "https://example.com/avatar.png",
                        Descendants = 0,
                        IsRoot = false,
                    }
                ],
                
                Addr = "E...",
                PlaceNumber = 1,
                Clone = 0,
                CreatedAt = 123456,
                Login = "bob",
                ImageUrl = "https://example.com/avatar.png",
                Descendants = 2,
                IsRoot = true,
                
            };
        });
    }

    public override async Task HandleAsync(GetTreeRequest request, CancellationToken ct)
    {
        var query = new GetTreeQuery(request.ProfileAddr, request.PlaceAddr);
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