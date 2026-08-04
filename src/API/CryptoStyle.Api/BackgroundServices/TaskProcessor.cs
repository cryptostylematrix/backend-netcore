using Contracts.Application.Abstractions;
using Contracts.Application.Features.ProfileItem;
using Contracts.Dto;
using MediatR;
using Microsoft.Extensions.Options;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Invites;
using TonSdk.Core.Boc;

namespace CryptoStyle.Api.BackgroundServices;

public sealed class TaskProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<TaskProcessorOptions> options,
    ILogger<TaskProcessor> logger) : BackgroundService
{
    private const uint UserCommandTaskTag = 0x48b18cdf;
    private const uint SystemCommandTaskTag = 0x5bfcb9f2;
    private const uint BonusQueryTaskTag = 0x24a2cffa;
    private const uint ProfileInfoQueryTaskTag = 0xf1e4dc7b;

    private const uint ActivatePlaceCommandTag = 0xf63f29c5;
    private const uint BuyFirstPlaceCommandTag = 0xd7dd1e7a;
    private const uint BuyPlaceCommandTag = 0xb070143f;
    private const uint BuySystemPlaceCommandTag = 0xe9cfbb76;
    private const uint BuyTopPlaceCommandTag = 0x3f6eb1fa;
    private const uint ChooseInviterCommandTag = 0xbc13b755;
    private const uint LockPositionCommandTag = 0x6292cd93;
    private const uint UnlockPositionCommandTag = 0xcc64122d;

    private const uint CreateCloneCommandTag = 0xca8b8aa2;
    private const uint CreateReinvestCloneCommandTag = 0x08b738b1;

    private readonly TimeSpan _interval = TimeSpan.FromSeconds(options.Value.IntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTasksAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Task Processor cycle failed.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessTasksAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var referalProgramQueries = scope.ServiceProvider.GetRequiredService<IReferalProgramQueries>();
        var marketingV3Queries = scope.ServiceProvider.GetRequiredService<IMarketingV3Queries>();
        var transactionSender = scope.ServiceProvider.GetRequiredService<IMarketingTransactionSender>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var programs = await referalProgramQueries.GetAllAsync(cancellationToken);

        foreach (var program in programs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var taskResult = await marketingV3Queries.GetFirstTaskAsync(
                program.MarketingAddr,
                cancellationToken);

            if (taskResult.IsSuccess && taskResult.Value is { Key: not null, Val: not null })
                await ProcessMarketingTaskAsync(
                    program.MarketingAddr,
                    taskResult.Value.Key.Value,
                    taskResult.Value.Val,
                    marketingV3Queries,
                    transactionSender,
                    sender,
                    cancellationToken);
        }
    }

    private async Task ProcessMarketingTaskAsync(
        string marketingAddr,
        uint taskKey,
        MarketingV3TaskResponse task,
        IMarketingV3Queries marketingV3Queries,
        IMarketingTransactionSender transactionSender,
        ISender sender,
        CancellationToken cancellationToken)
    {
        switch (task)
        {
            case { Command: { Tag: UserCommandTaskTag } userCommand }:
                await ProcessUserCommandAsync(
                    marketingAddr,
                    taskKey,
                    task,
                    userCommand,
                    marketingV3Queries,
                    transactionSender,
                    sender,
                    cancellationToken);
                break;

            case { Command: { Tag: SystemCommandTaskTag } systemCommand }:
                await ProcessSystemCommandAsync(taskKey, task.QueryId, systemCommand, cancellationToken);
                break;

            case { Query: { Tag: BonusQueryTaskTag } }:
                // TODO: Process bonus query task.
                break;

            case { Query: { Tag: ProfileInfoQueryTaskTag } }:
                // TODO: Process profile-info query task.
                break;

            default:
                // TODO: Process an unknown or empty marketing task.
                break;
        }
    }

    private static async Task ProcessUserCommandAsync(
        string marketingAddr,
        uint taskKey,
        MarketingV3TaskResponse task,
        MarketingV3TaskCommandResponse command,
        IMarketingV3Queries marketingV3Queries,
        IMarketingTransactionSender transactionSender,
        ISender sender,
        CancellationToken cancellationToken)
    {
        switch (command.CommandTag)
        {
            case ActivatePlaceCommandTag:
                // TODO: Process activate-place command.
                break;

            case BuyFirstPlaceCommandTag:
                // TODO: Process buy-first-place command.
                break;

            case BuyPlaceCommandTag:
                // TODO: Process buy-place command.
                break;

            case BuySystemPlaceCommandTag:
                // TODO: Process buy-system-place command.
                break;

            case BuyTopPlaceCommandTag:
                // TODO: Process buy-top-place command.
                break;

            case ChooseInviterCommandTag:
            {
                var parameters = DeserializeChooseInviterCommand(task, command);

                var profileAddr = parameters.ProfileAddr;
                if (string.IsNullOrWhiteSpace(profileAddr))
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        marketingV3Queries,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        "Choose-inviter command has no profile address.",
                        cancellationToken);
                    break;
                }

                var profileInfoResult = await sender.Send(
                    new GetNftDataQuery(profileAddr),
                    cancellationToken);

                if (!profileInfoResult.IsSuccess)
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        marketingV3Queries,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        ErrorComment(profileInfoResult.Errors, "Could not get profile info."),
                        cancellationToken);
                    break;
                }

                var profileLogin = profileInfoResult.Value.Content?.Login;
                if (string.IsNullOrWhiteSpace(profileLogin))
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        marketingV3Queries,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        "Profile info has no login.",
                        cancellationToken);
                    break;
                }

                var result = await sender.Send(
                    new ChooseInviterCommand(
                        MarketingAddr: marketingAddr,
                        InviterAddr: parameters.InviterProfileAddr,
                        ProfileAddr: profileAddr,
                        TaskKey: checked((int)taskKey),
                        QueryId: checked((long)task.QueryId),
                        SourceAddr: parameters.SourceAddr,
                        ProfileLogin: profileLogin),
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        marketingV3Queries,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        ErrorComment(result.Errors, "Could not choose inviter."),
                        cancellationToken);
                    break;
                }

                var createdPlace = result.Value;
                var response = marketingV3Queries.SendCommandResponse(
                    task.QueryId,
                    taskKey,
                    code: 0,
                    source: new MarketingV3SourcePlace
                    {
                        Place = new MarketingV3PlaceRef
                        {
                            Struct = createdPlace.StructNumber,
                            ProfileAddr = createdPlace.ProfileAddr,
                            PlaceNumber = createdPlace.PlaceNumber
                        },
                        ProfileLogin = createdPlace.ProfileLogin
                    });

                if (!response.IsSuccess)
                    throw new InvalidOperationException(
                        $"Could not build command response: {string.Join(", ", response.Errors)}");

                await transactionSender.SendAsync(
                    marketingAddr,
                    taskKey,
                    response.Value.BocHex,
                    cancellationToken);
                break;
            }

            case LockPositionCommandTag:
                // TODO: Process lock-position command.
                break;

            case UnlockPositionCommandTag:
                // TODO: Process unlock-position command.
                break;

            default:
                // TODO: Process an unknown user command.
                break;
        }
    }

    private static ChooseInviterCommandParameters DeserializeChooseInviterCommand(
        MarketingV3TaskResponse task,
        MarketingV3TaskCommandResponse command)
    {
        if (command.Struct is null)
            throw new InvalidOperationException("Choose-inviter command is missing its structure number.");

        if (command.Amount is null)
            throw new InvalidOperationException("Choose-inviter command is missing its amount.");

        if (string.IsNullOrWhiteSpace(task.PayloadBocHex))
            throw new InvalidOperationException("Choose-inviter command is missing its payload.");

        var payload = Cell.From(new Bits(Convert.FromHexString(task.PayloadBocHex)));
        var payloadSlice = payload.Parse();
        var inviterProfileAddr = payloadSlice.LoadAddress()?.ToString()
            ?? throw new InvalidOperationException(
                "Choose-inviter payload is missing the inviter profile address.");

        if (payloadSlice.RemainderBits != 0 || payloadSlice.RemainderRefs != 0)
            throw new InvalidOperationException("Choose-inviter payload contains trailing data.");

        return new ChooseInviterCommandParameters(
            QueryId: task.QueryId,
            Struct: command.Struct.Value,
            CommandTag: command.CommandTag,
            ProfileAddr: command.ProfileAddr,
            SourceAddr: command.SourceAddr,
            Amount: command.Amount.Value,
            SenderJettonWallet: command.SenderJettonWallet,
            InviterProfileAddr: inviterProfileAddr);
    }

    private static async Task CancelTaskAsync(
        string marketingAddr,
        IMarketingV3Queries marketingV3Queries,
        IMarketingTransactionSender transactionSender,
        ulong queryId,
        uint taskKey,
        string comment,
        CancellationToken cancellationToken)
    {
        var result = marketingV3Queries.SendCancelTask(queryId, taskKey, comment);

        if (!result.IsSuccess)
            throw new InvalidOperationException(
                $"Could not build task cancellation: {string.Join(", ", result.Errors)}");

        await transactionSender.SendAsync(
            marketingAddr,
            taskKey,
            result.Value.BocHex,
            cancellationToken);
    }

    private static string ErrorComment(
        IEnumerable<string> errors,
        string fallback)
    {
        var comment = string.Join(", ", errors);
        return string.IsNullOrWhiteSpace(comment) ? fallback : comment;
    }

    private static Task ProcessSystemCommandAsync(
        uint taskKey,
        ulong queryId,
        MarketingV3TaskCommandResponse command,
        CancellationToken cancellationToken)
    {
        switch (command.CommandTag)
        {
            case CreateCloneCommandTag:
                // TODO: Process create-clone command.
                break;

            case CreateReinvestCloneCommandTag:
                // TODO: Process create-reinvest-clone command.
                break;

            default:
                // TODO: Process an unknown system command.
                break;
        }

        return Task.CompletedTask;
    }

    private sealed record ChooseInviterCommandParameters(
        ulong QueryId,
        byte Struct,
        uint CommandTag,
        string? ProfileAddr,
        string? SourceAddr,
        ulong Amount,
        string? SenderJettonWallet,
        string InviterProfileAddr);
}
