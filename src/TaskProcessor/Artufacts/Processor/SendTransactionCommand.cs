using TonSdk.Contracts;

namespace Contracts.Application.Features.Processor;

public sealed record SendTransactionCommand() : ICommand;

internal sealed class SendTransactionCommandHandler(ITonClient tonClient, IMapper mapper) : 
    ICommandHandler<SendTransactionCommand>
{
    public async Task<Result> Handle(SendTransactionCommand request, CancellationToken ct)
    {
        // var wallet = new Wal
        //
        // var inviteContract = PlaceContract.CreateFromAddress(new Address(request.Addr));
        // var result = await inviteContract.GetPlaceData(tonClient, ct);
        //
        // if (!result.IsSuccess)
        // {
        //     return Result<PlaceDataResponse>.Error(new ErrorList(result.Errors));
        // }
        //  
        // var response = mapper.Map<PlaceDataResponse>(result.Value);
        // return Result.Success(response);
        throw new NotImplementedException();
    }
}