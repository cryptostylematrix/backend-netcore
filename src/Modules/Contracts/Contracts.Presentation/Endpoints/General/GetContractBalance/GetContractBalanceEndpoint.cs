using Contracts.Application.Features.General;

namespace Contracts.Presentation.Endpoints.General.GetContractBalance;

public sealed class GetContractBalanceEndpoint(ISender sender) : Endpoint<GetContractBalanceRequest, ContractBalanceResponse>
{
    public override void Configure()
    {
        Get("contracts/general/{addr}/balance");
        Tags("Contracts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Contract balance";
            s.Description = "Get Contract balance";
            s.ExampleRequest = new GetContractBalanceRequest
            {
                Addr = "E..."
            };
            s.ResponseExamples[StatusCodes.Status200OK] = new ContractBalanceResponse
            {
               Balance = 123
            };
        });
    }

    public override async Task HandleAsync(GetContractBalanceRequest request, CancellationToken ct)
    {
        var query = new GetContractBalanceQuery(request.Addr);
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