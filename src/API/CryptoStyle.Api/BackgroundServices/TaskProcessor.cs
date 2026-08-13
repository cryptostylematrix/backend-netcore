using Contracts.Application.Features.MarketingV3;
using Contracts.Application.Features.ProfileItem;
using Contracts.Dto;
using IMarketingTransactionSender = Contracts.Application.Abstractions.IMarketingTransactionSender;
using MediatR;
using Microsoft.Extensions.Options;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Features.Invites;
using ReferalProgram.Application.Features.Locks;
using ReferalProgram.Application.Features.Places;
using ReferalProgram.Dto;
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
    private const uint StructBonusTag = 0xe1319040;

    private readonly TimeSpan _interval = TimeSpan.FromSeconds(options.Value.IntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                LogAction("Polling cycle started");
                await ProcessTasksAsync(stoppingToken);
                LogAction("Polling cycle completed");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "[API TaskProcessor] Polling cycle failed");
            }

            LogAction("Waiting for the next polling cycle");
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessTasksAsync(CancellationToken cancellationToken)
    {
        LogAction("Creating task-processing service scope");
        await using var scope = scopeFactory.CreateAsyncScope();
        var referalProgramQueries = scope.ServiceProvider.GetRequiredService<IReferalProgramQueries>();
        var transactionSender = scope.ServiceProvider.GetRequiredService<IMarketingTransactionSender>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        LogAction("Loading referral programs");
        var programs = await referalProgramQueries.GetAllAsync(cancellationToken);
        logger.LogInformation(
            "[API TaskProcessor] Loaded {ProgramCount} referral programs",
            programs.Count);

        foreach (var program in programs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation(
                "[API TaskProcessor] Requesting first task for marketing {MarketingAddr}",
                program.MarketingAddr);

            var taskResult = await sender.Send(
                new GetFirstTaskQuery(program.MarketingAddr),
                cancellationToken);

            if (taskResult.IsSuccess && taskResult.Value is { Key: not null, Val: not null })
            {
                logger.LogInformation(
                    "[API TaskProcessor] Task {TaskKey} received for marketing {MarketingAddr}",
                    taskResult.Value.Key.Value,
                    program.MarketingAddr);
                await ProcessMarketingTaskAsync(
                    program.MarketingAddr,
                    taskResult.Value.Key.Value,
                    taskResult.Value.Val,
                    transactionSender,
                    sender,
                    cancellationToken);
            }
            else if (!taskResult.IsSuccess)
            {
                logger.LogWarning(
                    "[API TaskProcessor] Failed to request task for marketing {MarketingAddr}: {Errors}",
                    program.MarketingAddr,
                    string.Join(", ", taskResult.Errors));
            }
            else
            {
                logger.LogInformation(
                    "[API TaskProcessor] No pending task for marketing {MarketingAddr}",
                    program.MarketingAddr);
            }
        }
    }

    private async Task ProcessMarketingTaskAsync(
        string marketingAddr,
        uint taskKey,
        MarketingV3TaskResponse task,
        IMarketingTransactionSender transactionSender,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var taskKind = task.Command is not null ? "Command"
            : task.Query is not null ? "Query"
            : "Unknown";
        var taskTag = task.Command?.CommandTag
            ?? task.Query?.BonusTypeTag
            ?? 0;

        using var logScope = logger.BeginScope(new Dictionary<string, object>
        {
            ["MarketingAddr"] = marketingAddr,
            ["TaskKey"] = taskKey,
            ["QueryId"] = task.QueryId,
            ["TaskKind"] = taskKind,
            ["TaskTag"] = $"0x{taskTag:x8}"
        });

        LogAction("Marketing task processing started");

        try
        {
            switch (task)
            {
                case
                {
                    Command: { Tag: SystemCommandTaskTag } systemCommand,
                    Query: { Tag: BonusQueryTaskTag } bonusQuery
                }:
                    LogAction("Dispatching move-or-structure-bonus task");
                    await ProcessMoveOrStructBonusAsync(
                        marketingAddr,
                        taskKey,
                        task,
                        systemCommand,
                        bonusQuery,
                        transactionSender,
                        sender,
                        cancellationToken);
                    break;

                case { Command: { Tag: UserCommandTaskTag } userCommand }:
                    LogAction("Dispatching user command task");
                    await ProcessUserCommandAsync(
                        marketingAddr,
                        taskKey,
                        task,
                        userCommand,
                        transactionSender,
                        sender,
                        cancellationToken);
                    break;

                case { Command: { Tag: SystemCommandTaskTag } systemCommand }:
                    LogAction("Dispatching system command task");
                    await ProcessSystemCommandAsync(
                        marketingAddr,
                        taskKey,
                        task,
                        systemCommand,
                        transactionSender,
                        sender,
                        cancellationToken);
                    break;

                case { Query: { Tag: BonusQueryTaskTag } bonusQuery }:
                    LogAction("Dispatching bonus query task");
                    await ProcessBonusQueryAsync(
                        marketingAddr,
                        taskKey,
                        task,
                        bonusQuery,
                        transactionSender,
                        sender,
                        cancellationToken);
                    break;

                case { Query: { Tag: ProfileInfoQueryTaskTag } profileInfoQuery }:
                    LogAction("Dispatching profile-info query task");
                    await ProcessProfileInfoQueryAsync(
                        marketingAddr,
                        taskKey,
                        task,
                        profileInfoQuery,
                        transactionSender,
                        sender,
                        cancellationToken);
                    break;

                default:
                    logger.LogWarning("[API TaskProcessor] Marketing task type is not supported");
                    break;
            }

            LogAction("Marketing task processing completed");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "[API TaskProcessor] Marketing task processing failed");
            throw;
        }
    }

    private async Task ProcessUserCommandAsync(
        string marketingAddr,
        uint taskKey,
        MarketingV3TaskResponse task,
        MarketingV3TaskCommandResponse command,
        IMarketingTransactionSender transactionSender,
        ISender sender,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[API TaskProcessor] Processing user command {CommandTag}",
            $"0x{command.CommandTag:x8}");

        switch (command.CommandTag)
        {
            case ActivatePlaceCommandTag:
                logger.LogWarning("[API TaskProcessor] Activate-place command is not implemented");
                // TODO: Process activate-place command.
                break;

            case BuyFirstPlaceCommandTag:
            case BuyPlaceCommandTag:
            case BuyTopPlaceCommandTag:
            {
                var commandName = command.CommandTag switch
                {
                    BuyFirstPlaceCommandTag => "buy-first-place",
                    BuyTopPlaceCommandTag => "buy-top-place",
                    _ => "buy-place"
                };

                LogAction($"Validating {commandName} command");
                if (command.Struct is null)
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        $"{commandName} command is missing its structure number.",
                        cancellationToken);
                    break;
                }

                if (string.IsNullOrWhiteSpace(command.ProfileAddr))
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        $"{commandName} command has no profile address.",
                        cancellationToken);
                    break;
                }

                LogAction("Loading buyer profile information");
                var profileInfoResult = await sender.Send(
                    new GetNftDataQuery(command.ProfileAddr),
                    cancellationToken);

                if (!profileInfoResult.IsSuccess)
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
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
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        "Profile info has no login.",
                        cancellationToken);
                    break;
                }

                LogAction($"Deserializing {commandName} child position");
                var childPosition = DeserializeChildPosition(task.PayloadBocHex);
                LogAction($"Executing {commandName} application command");
                var result = await sender.Send(
                    new BuyPlaceCommand(
                        MarketingAddr: marketingAddr,
                        StructureNumber: command.Struct.Value,
                        ProfileAddr: command.ProfileAddr,
                        ProfileLogin: profileLogin,
                        TaskKey: checked((int)taskKey),
                        QueryId: checked((long)task.QueryId),
                        SourceAddr: command.SourceAddr,
                        Kind: command.CommandTag switch
                        {
                            BuyFirstPlaceCommandTag => BuyPlaceKind.First,
                            BuyTopPlaceCommandTag => BuyPlaceKind.Top,
                            _ => BuyPlaceKind.Regular
                        },
                        ChildPosition: childPosition),
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        ErrorComment(result.Errors, $"Could not execute {commandName}."),
                        cancellationToken);
                    break;
                }

                LogAction($"{commandName} application command completed");
                var sourcePlace = result.Value.Source;
                LogAction($"Building {commandName} command response");
                var response = await sender.Send(
                    new BuildCommandResponseQuery(
                        task.QueryId,
                        taskKey,
                        result.Value.Code,
                        new MarketingV3SourcePlace
                        {
                            Place = new MarketingV3PlaceRef
                            {
                                Struct = sourcePlace.StructNumber,
                                ProfileAddr = sourcePlace.ProfileAddr,
                                PlaceNumber = sourcePlace.PlaceNumber
                            },
                            ProfileLogin = sourcePlace.ProfileLogin
                        }),
                    cancellationToken);

                if (!response.IsSuccess)
                    throw new InvalidOperationException(
                        $"Could not build command response: {string.Join(", ", response.Errors)}");

                LogAction($"Sending {commandName} command response transaction");
                await transactionSender.SendAsync(
                    marketingAddr,
                    taskKey,
                    response.Value.BocHex,
                    cancellationToken);
                LogAction($"{commandName} command response transaction sent");
                break;
            }

            case BuySystemPlaceCommandTag:
            {
                LogAction("Validating buy-system-place command");
                if (command.Struct is null)
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        "Buy-system-place command is missing its structure number.",
                        cancellationToken);
                    break;
                }


                LogAction("Deserializing buy-system-place child position");
                var childPosition = DeserializeChildPosition(task.PayloadBocHex);
                LogAction("Executing buy-system-place application command");
                var result = await sender.Send(
                    new BuySystemPlaceCommand(
                        MarketingAddr: marketingAddr,
                        StructureNumber: command.Struct.Value,

                        TaskKey: checked((int)taskKey),
                        QueryId: checked((long)task.QueryId),
                        SourceAddr: command.SourceAddr,
                        ChildPosition: childPosition),
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        ErrorComment(result.Errors, "Could not buy place."),
                        cancellationToken);
                    break;
                }

                LogAction("Buy-system-place application command completed");
                var sourcePlace = result.Value.Source;
                LogAction("Building buy-system-place command response");
                var response = await sender.Send(
                    new BuildCommandResponseQuery(
                        task.QueryId,
                        taskKey,
                        result.Value.Code,
                        new MarketingV3SourcePlace
                        {
                            Place = new MarketingV3PlaceRef
                            {
                                Struct = sourcePlace.StructNumber,
                                ProfileAddr = sourcePlace.ProfileAddr,
                                PlaceNumber = sourcePlace.PlaceNumber
                            },
                            ProfileLogin = sourcePlace.ProfileLogin
                        }),
                    cancellationToken);

                if (!response.IsSuccess)
                    throw new InvalidOperationException(
                        $"Could not build command response: {string.Join(", ", response.Errors)}");

                LogAction("Sending buy-system-place command response transaction");
                await transactionSender.SendAsync(
                    marketingAddr,
                    taskKey,
                    response.Value.BocHex,
                    cancellationToken);
                LogAction("Buy-system-place command response transaction sent");
                break;
            }

            case ChooseInviterCommandTag:
            {
                LogAction("Deserializing choose-inviter command");
                var parameters = DeserializeChooseInviterCommand(task, command);

                var profileAddr = parameters.ProfileAddr;
                if (string.IsNullOrWhiteSpace(profileAddr))
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        "Choose-inviter command has no profile address.",
                        cancellationToken);
                    break;
                }

                LogAction("Loading invite profile information");
                var profileInfoResult = await sender.Send(
                    new GetNftDataQuery(profileAddr),
                    cancellationToken);

                if (!profileInfoResult.IsSuccess)
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
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
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        "Profile info has no login.",
                        cancellationToken);
                    break;
                }

                LogAction("Executing choose-inviter application command");
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
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        ErrorComment(result.Errors, "Could not choose inviter."),
                        cancellationToken);
                    break;
                }

                LogAction("Choose-inviter application command completed");
                var sourcePlace = result.Value.Source;
                LogAction("Building choose-inviter command response");
                var response = await sender.Send(
                    new BuildCommandResponseQuery(
                        task.QueryId,
                        taskKey,
                        result.Value.Code,
                        new MarketingV3SourcePlace
                        {
                            Place = new MarketingV3PlaceRef
                            {
                                Struct = sourcePlace.StructNumber,
                                ProfileAddr = sourcePlace.ProfileAddr,
                                PlaceNumber = sourcePlace.PlaceNumber
                            },
                            ProfileLogin = sourcePlace.ProfileLogin
                        }),
                    cancellationToken);

                if (!response.IsSuccess)
                    throw new InvalidOperationException(
                        $"Could not build command response: {string.Join(", ", response.Errors)}");

                LogAction("Sending choose-inviter command response transaction");
                await transactionSender.SendAsync(
                    marketingAddr,
                    taskKey,
                    response.Value.BocHex,
                    cancellationToken);
                LogAction("Choose-inviter command response transaction sent");
                break;
            }

            case LockPositionCommandTag:
            {
                LogAction("Validating lock-position command");
                if (command.Struct is null
                    || string.IsNullOrWhiteSpace(command.ProfileAddr))
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        "Lock-position command is missing its structure or profile address.",
                        cancellationToken);
                    break;
                }

                if (!TryDeserializeRequiredPosition(
                        task.PayloadBocHex,
                        "Lock-position",
                        out var position,
                        out var positionError))
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        positionError,
                        cancellationToken);
                    break;
                }

                var result = await sender.Send(
                    new LockPositionCommand(
                        MarketingAddr: marketingAddr,
                        StructureNumber: command.Struct.Value,
                        PlaceStructureNumber: position.Parent.StructureNumber,
                        ProfileAddr: command.ProfileAddr,
                        PlaceProfileAddr: position.Parent.ProfileAddr,
                        PlaceNumber: position.Parent.PlaceNumber,
                        LockedPos: position.Position,
                        TaskKey: checked((int)taskKey),
                        QueryId: checked((long)task.QueryId),
                        SourceAddr: command.SourceAddr),
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        ErrorComment(result.Errors, "Could not lock the position."),
                        cancellationToken);
                    break;
                }

                await SendCommandResponseAsync(
                    marketingAddr,
                    task.QueryId,
                    taskKey,
                    result.Value,
                    sender,
                    transactionSender,
                    cancellationToken);
                break;
            }

            case UnlockPositionCommandTag:
            {
                LogAction("Validating unlock-position command");
                if (command.Struct is null
                    || string.IsNullOrWhiteSpace(command.ProfileAddr))
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        "Unlock-position command is missing its structure or profile address.",
                        cancellationToken);
                    break;
                }

                if (!TryDeserializeRequiredPosition(
                        task.PayloadBocHex,
                        "Unlock-position",
                        out var position,
                        out var positionError))
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        positionError,
                        cancellationToken);
                    break;
                }

                var result = await sender.Send(
                    new UnlockPositionCommand(
                        MarketingAddr: marketingAddr,
                        StructureNumber: command.Struct.Value,
                        PlaceStructureNumber: position.Parent.StructureNumber,
                        ProfileAddr: command.ProfileAddr,
                        PlaceProfileAddr: position.Parent.ProfileAddr,
                        PlaceNumber: position.Parent.PlaceNumber,
                        LockedPos: position.Position,
                        SourceAddr: command.SourceAddr),
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        ErrorComment(result.Errors, "Could not unlock the position."),
                        cancellationToken);
                    break;
                }

                await SendCommandResponseAsync(
                    marketingAddr,
                    task.QueryId,
                    taskKey,
                    result.Value,
                    sender,
                    transactionSender,
                    cancellationToken);
                break;
            }

            default:
                logger.LogWarning(
                    "[API TaskProcessor] User command {CommandTag} is not supported",
                    $"0x{command.CommandTag:x8}");
                // TODO: Process an unknown user command.
                break;
        }
    }

    private async Task ProcessBonusQueryAsync(
        string marketingAddr,
        uint taskKey,
        MarketingV3TaskResponse task,
        MarketingV3TaskQueryResponse query,
        IMarketingTransactionSender transactionSender,
        ISender sender,
        CancellationToken cancellationToken)
    {
        LogAction("Validating bonus query");
        if (query.Relative?.Source is not { } relativePlace)
        {
            await CancelTaskAsync(
                marketingAddr,
                sender,
                transactionSender,
                task.QueryId,
                taskKey,
                "Bonus query has no relative place.",
                cancellationToken);
            return;
        }

        LogAction("Resolving bonus reason and recipient");
        var bonusResult = await sender.Send(
            new ResolveBonusQuery(
                MarketingAddr: marketingAddr,
                BonusTypeTag: query.BonusTypeTag,
                StructureNumber: relativePlace.Struct,
                RelativeProfileAddr: relativePlace.ProfileAddr,
                RelativePlaceNumber: relativePlace.PlaceNumber,
                Level: query.Relative.Level),
            cancellationToken);

        if (!bonusResult.IsSuccess)
        {
            await CancelTaskAsync(
                marketingAddr,
                sender,
                transactionSender,
                task.QueryId,
                taskKey,
                ErrorComment(bonusResult.Errors, "Could not resolve bonus query."),
                cancellationToken);
            return;
        }

        LogAction("Bonus reason and recipient resolved");
        LogAction("Loading bonus recipient profile information");
        var profileResult = await sender.Send(
            new GetNftDataQuery(bonusResult.Value.RecipientProfileAddr),
            cancellationToken);

        if (!profileResult.IsSuccess)
        {
            await CancelTaskAsync(
                marketingAddr,
                sender,
                transactionSender,
                task.QueryId,
                taskKey,
                ErrorComment(profileResult.Errors, "Could not get bonus recipient profile info."),
                cancellationToken);
            return;
        }

        var profileLogin = profileResult.Value.Content?.Login;
        if (string.IsNullOrWhiteSpace(profileLogin))
        {
            await CancelTaskAsync(
                marketingAddr,
                sender,
                transactionSender,
                task.QueryId,
                taskKey,
                "Bonus recipient profile has no login.",
                cancellationToken);
            return;
        }

        LogAction("Building bonus query response");
        var response = await sender.Send(
            new BuildBonusQueryResponseQuery(
                task.QueryId,
                taskKey,
                new MarketingV3PlaceInfo
                {
                    PlaceNumber = bonusResult.Value.Reason.PlaceNumber,
                    ProfileLogin = bonusResult.Value.Reason.ProfileLogin
                },
                new MarketingV3ProfileData
                {
                    ProfileAddr = bonusResult.Value.RecipientProfileAddr,
                    ProfileLogin = profileLogin,
                    OwnerAddr = profileResult.Value.OwnerAddr
                }),
            cancellationToken);

        if (!response.IsSuccess)
            throw new InvalidOperationException(
                $"Could not build bonus query response: {string.Join(", ", response.Errors)}");

        LogAction("Sending bonus query response transaction");
        await transactionSender.SendAsync(
            marketingAddr,
            taskKey,
            response.Value.BocHex,
            cancellationToken);
        LogAction("Bonus query response transaction sent");
    }

    private async Task ProcessProfileInfoQueryAsync(
        string marketingAddr,
        uint taskKey,
        MarketingV3TaskResponse task,
        MarketingV3TaskQueryResponse query,
        IMarketingTransactionSender transactionSender,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.RecipientProfileAddr))
        {
            await CancelTaskAsync(
                marketingAddr,
                sender,
                transactionSender,
                task.QueryId,
                taskKey,
                "Profile-info query has no recipient profile address.",
                cancellationToken);
            return;
        }

        LogAction("Loading profile-info query recipient");
        var profileResult = await sender.Send(
            new GetNftDataQuery(query.RecipientProfileAddr),
            cancellationToken);

        if (!profileResult.IsSuccess)
        {
            await CancelTaskAsync(
                marketingAddr,
                sender,
                transactionSender,
                task.QueryId,
                taskKey,
                ErrorComment(profileResult.Errors, "Could not get recipient profile info."),
                cancellationToken);
            return;
        }

        var profileLogin = profileResult.Value.Content?.Login;
        if (string.IsNullOrWhiteSpace(profileLogin))
        {
            await CancelTaskAsync(
                marketingAddr,
                sender,
                transactionSender,
                task.QueryId,
                taskKey,
                "Profile-info query recipient has no login.",
                cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(profileResult.Value.OwnerAddr))
        {
            await CancelTaskAsync(
                marketingAddr,
                sender,
                transactionSender,
                task.QueryId,
                taskKey,
                "Profile-info query recipient has no owner address.",
                cancellationToken);
            return;
        }

        LogAction("Building profile-info query response");
        var response = await sender.Send(
            new BuildProfileInfoQueryResponseQuery(
                task.QueryId,
                taskKey,
                new MarketingV3ProfileInfo
                {
                    ProfileLogin = profileLogin,
                    OwnerAddr = profileResult.Value.OwnerAddr
                }),
            cancellationToken);

        if (!response.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Could not build profile-info query response: {string.Join(", ", response.Errors)}");
        }

        LogAction("Sending profile-info query response transaction");
        await transactionSender.SendAsync(
            marketingAddr,
            taskKey,
            response.Value.BocHex,
            cancellationToken);
        LogAction("Profile-info query response transaction sent");
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

    private static ChildPosition? DeserializeChildPosition(string? payloadBocHex)
    {
        if (string.IsNullOrWhiteSpace(payloadBocHex))
            return null;

        var payload = Cell.From(new Bits(Convert.FromHexString(payloadBocHex)));
        var slice = payload.Parse();
        var parent = new BuyPlaceRef(
            StructureNumber: checked((byte)slice.LoadUInt(8)),
            ProfileAddr: slice.LoadAddress()?.ToString(),
            PlaceNumber: checked((uint)slice.LoadUInt(32)));
        var position = checked((uint)slice.LoadUInt(32));

        if (slice.RemainderBits != 0 || slice.RemainderRefs != 0)
            throw new InvalidOperationException("Buy-place payload contains trailing data.");

        return new ChildPosition(parent, position);
    }

    private static bool TryDeserializeRequiredPosition(
        string? payloadBocHex,
        string commandName,
        out ChildPosition position,
        out string error)
    {
        try
        {
            var parsed = DeserializeChildPosition(payloadBocHex);
            if (parsed is null)
            {
                position = null!;
                error = $"{commandName} command is missing its position payload.";
                return false;
            }

            position = parsed;
            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            position = null!;
            error = $"{commandName} position payload is invalid: {exception.Message}";
            return false;
        }
    }

    private async Task CancelTaskAsync(
        string marketingAddr,
        ISender sender,
        IMarketingTransactionSender transactionSender,
        ulong queryId,
        uint taskKey,
        string comment,
        CancellationToken cancellationToken)
    {
        LogAction("Building task cancellation response");
        var result = await sender.Send(
            new BuildCancelTaskQuery(queryId, taskKey, comment),
            cancellationToken);

        if (!result.IsSuccess)
            throw new InvalidOperationException(
                $"Could not build task cancellation: {string.Join(", ", result.Errors)}");

        logger.LogWarning(
            "[API TaskProcessor] Marketing task cancelled: {CancellationComment}",
            comment);

        LogAction("Sending task cancellation transaction");
        await transactionSender.SendAsync(
            marketingAddr,
            taskKey,
            result.Value.BocHex,
            cancellationToken);
        LogAction("Task cancellation transaction sent");
    }

    private async Task SendCommandResponseAsync(
        string marketingAddr,
        ulong queryId,
        uint taskKey,
        CommandResponse commandResponse,
        ISender sender,
        IMarketingTransactionSender transactionSender,
        CancellationToken cancellationToken)
    {
        var source = commandResponse.Source;
        var response = await sender.Send(
            new BuildCommandResponseQuery(
                queryId,
                taskKey,
                commandResponse.Code,
                new MarketingV3SourcePlace
                {
                    Place = new MarketingV3PlaceRef
                    {
                        Struct = source.StructNumber,
                        ProfileAddr = source.ProfileAddr,
                        PlaceNumber = source.PlaceNumber
                    },
                    ProfileLogin = source.ProfileLogin
                }),
            cancellationToken);

        if (!response.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Could not build command response: {string.Join(", ", response.Errors)}");
        }

        await transactionSender.SendAsync(
            marketingAddr,
            taskKey,
            response.Value.BocHex,
            cancellationToken);
    }

    private static string ErrorComment(
        IEnumerable<string> errors,
        string fallback)
    {
        var comment = string.Join(", ", errors);
        return string.IsNullOrWhiteSpace(comment) ? fallback : comment;
    }

    private async Task ProcessMoveOrStructBonusAsync(
        string marketingAddr,
        uint taskKey,
        MarketingV3TaskResponse task,
        MarketingV3TaskCommandResponse command,
        MarketingV3TaskQueryResponse query,
        IMarketingTransactionSender transactionSender,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (command.CommandTag != CreateCloneCommandTag
            || query.BonusTypeTag != StructBonusTag)
        {
            await CancelTaskAsync(
                marketingAddr,
                sender,
                transactionSender,
                task.QueryId,
                taskKey,
                "Combined task must contain create-clone and structure-bonus operations.",
                cancellationToken);
            return;
        }

        if (command.CommandStruct is null
            || command.Relative?.Source is not { } commandSource
            || query.Relative?.Source is not { } querySource)
        {
            await CancelTaskAsync(
                marketingAddr,
                sender,
                transactionSender,
                task.QueryId,
                taskKey,
                "Move-or-structure-bonus task is missing its structure or relative place.",
                cancellationToken);
            return;
        }

        var relativeMatches = command.Relative.Level == query.Relative.Level
            && commandSource.Struct == querySource.Struct
            && commandSource.PlaceNumber == querySource.PlaceNumber
            && string.Equals(
                commandSource.ProfileAddr,
                querySource.ProfileAddr,
                StringComparison.Ordinal);
        if (!relativeMatches)
        {
            await CancelTaskAsync(
                marketingAddr,
                sender,
                transactionSender,
                task.QueryId,
                taskKey,
                "Move-or-structure-bonus command and query reference different places.",
                cancellationToken);
            return;
        }

        var decision = await sender.Send(
            new ResolveMoveOrStructBonusQuery(
                MarketingAddr: marketingAddr,
                TargetStructureNumber: command.CommandStruct.Value,
                SourceStructureNumber: commandSource.Struct,
                SourceProfileAddr: commandSource.ProfileAddr,
                SourcePlaceNumber: commandSource.PlaceNumber,
                RelativeLevel: command.Relative.Level,
                TaskKey: checked((int)taskKey)),
            cancellationToken);

        if (!decision.IsSuccess)
        {
            await CancelTaskAsync(
                marketingAddr,
                sender,
                transactionSender,
                task.QueryId,
                taskKey,
                ErrorComment(decision.Errors, "Could not resolve move-or-structure-bonus task."),
                cancellationToken);
            return;
        }

        if (decision.Value.CreateClone)
        {
            LogAction("Move-or-structure-bonus selected clone creation");
            await ProcessSystemCommandAsync(
                marketingAddr,
                taskKey,
                task,
                command,
                transactionSender,
                sender,
                cancellationToken);
            return;
        }

        LogAction("Move-or-structure-bonus selected structure bonus");
        await ProcessBonusQueryAsync(
            marketingAddr,
            taskKey,
            task,
            query,
            transactionSender,
            sender,
            cancellationToken);
    }

    private async Task ProcessSystemCommandAsync(
        string marketingAddr,
        uint taskKey,
        MarketingV3TaskResponse task,
        MarketingV3TaskCommandResponse command,
        IMarketingTransactionSender transactionSender,
        ISender sender,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[API TaskProcessor] Processing system command {CommandTag}",
            $"0x{command.CommandTag:x8}");

        switch (command.CommandTag)
        {
            case CreateCloneCommandTag:
            case CreateReinvestCloneCommandTag:
            {
                var commandName = command.CommandTag == CreateCloneCommandTag
                    ? "create-clone"
                    : "create-reinvest-clone";

                if (command.CommandStruct is null)
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        $"{commandName} command is missing its structure number.",
                        cancellationToken);
                    break;
                }

                if (command.Relative?.Source is null)
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        $"{commandName} command is missing its relative source place.",
                        cancellationToken);
                    break;
                }

                LogAction($"Executing {commandName} application command");
                var relative = command.Relative;
                var result = await sender.Send(
                    new CreateSystemCloneCommand(
                        MarketingAddr: marketingAddr,
                        StructureNumber: command.CommandStruct.Value,
                        SourceStructureNumber: relative.Source.Struct,
                        SourceProfileAddr: relative.Source.ProfileAddr,
                        SourcePlaceNumber: relative.Source.PlaceNumber,
                        RelativeLevel: relative.Level,
                        TaskKey: checked((int)taskKey),
                        QueryId: checked((long)task.QueryId)),
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    await CancelTaskAsync(
                        marketingAddr,
                        sender,
                        transactionSender,
                        task.QueryId,
                        taskKey,
                        ErrorComment(result.Errors, $"Could not execute {commandName}."),
                        cancellationToken);
                    break;
                }

                LogAction($"Sending {commandName} command response");
                await SendCommandResponseAsync(
                    marketingAddr,
                    task.QueryId,
                    taskKey,
                    result.Value,
                    sender,
                    transactionSender,
                    cancellationToken);
                LogAction($"{commandName} command response transaction sent");
                break;
            }

            default:
                await CancelTaskAsync(
                    marketingAddr,
                    sender,
                    transactionSender,
                    task.QueryId,
                    taskKey,
                    $"System command 0x{command.CommandTag:x8} is not supported.",
                    cancellationToken);
                break;
        }
    }

    private void LogAction(string action) =>
        logger.LogInformation("[API TaskProcessor] {Action}", action);

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
