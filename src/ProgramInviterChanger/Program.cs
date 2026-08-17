using ProgramInviterChanger;

try
{
    var settings = AppSettings.Load();
    using var cancellationSource = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellationSource.Cancel();
    };

    var marketingAddr = PromptRequired("Marketing address");
    var referralLogin = PromptRequired("Referral login").ToLowerInvariant();
    var newInviterLogin = PromptRequired("New inviter login").ToLowerInvariant();

    using var httpClient = new HttpClient
    {
        BaseAddress = settings.ApiBaseUrl,
        Timeout = TimeSpan.FromSeconds(60)
    };
    var contracts = new ContractsApiClient(httpClient, settings.RequestDelay);

    Console.WriteLine();
    Console.WriteLine("Resolving profiles through the contracts API...");
    var referralProfileAddr = await contracts.GetProfileAddressAsync(
        referralLogin,
        cancellationSource.Token);
    var newInviterProfileAddr = await contracts.GetProfileAddressAsync(
        newInviterLogin,
        cancellationSource.Token);

    if (string.Equals(referralProfileAddr, newInviterProfileAddr, StringComparison.Ordinal))
        throw new InvalidOperationException("A referral cannot be its own inviter.");

    var service = new InviterChangeService(settings.ConnectionString);
    var plan = await service.PlanAsync(
        marketingAddr,
        referralProfileAddr,
        newInviterProfileAddr,
        cancellationSource.Token);

    if (plan.NoChange)
    {
        Console.WriteLine(
            "No change is needed: '{0}' is already invited by '{1}'.",
            referralLogin,
            newInviterLogin);
        return 0;
    }

    Console.WriteLine();
    Console.WriteLine("Proposed change:");
    Console.WriteLine("  Program:       {0}", marketingAddr);
    Console.WriteLine("  Referral:      {0}", plan.ReferralLogin);
    Console.WriteLine("  Old inviter:   {0}", plan.OldInviterLogin);
    Console.WriteLine("  New inviter:   {0}", plan.NewInviterLogin);
    Console.WriteLine("  Places moved:  {0}", plan.SubtreePlaces);
    Console.WriteLine("  New position:  {0}", plan.NewPosition);
    Console.WriteLine();
    Console.Write("Type CHANGE to apply: ");
    if (!string.Equals(Console.ReadLine()?.Trim(), "CHANGE", StringComparison.Ordinal))
    {
        Console.WriteLine("Cancelled; no database changes were made.");
        return 0;
    }

    var result = await service.ChangeAsync(
        marketingAddr,
        referralProfileAddr,
        newInviterProfileAddr,
        cancellationSource.Token);
    if (result.NoChange)
    {
        Console.WriteLine("No change is needed; the inviter was already updated.");
        return 0;
    }

    Console.WriteLine(
        "Inviter changed successfully. Updated {0} places and {1} locks.",
        result.MovedPlaces,
        result.UpdatedLocks);
    return 0;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Operation cancelled.");
    return 130;
}
catch (Exception exception)
{
    Console.Error.WriteLine("Inviter change failed: {0}", exception.Message);
    return 1;
}

static string PromptRequired(string label)
{
    Console.Write($"{label}: ");
    var value = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"{label} is required.");

    return value.Trim();
}
