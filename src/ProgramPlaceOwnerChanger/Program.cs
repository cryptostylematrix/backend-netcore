using ProgramPlaceOwnerChanger;

try
{
    var settings = AppSettings.Load();
    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
    var marketing = Required("Marketing contract address");
    Console.Write("Structure (1–255; empty = all > 0): ");
    var structure = OwnerChangeRequest.ParseStructure(Console.ReadLine());
    var oldLogin = Required("Old login").ToLowerInvariant();
    var newLogin = Required("New login").ToLowerInvariant();
    using var http = new HttpClient { BaseAddress = settings.ApiBaseUrl, Timeout = TimeSpan.FromSeconds(60) };
    var contracts = new ContractsApiClient(http, settings.RequestDelay);
    var oldAddress = await contracts.GetProfileAddressAsync(oldLogin, cancellation.Token);
    var newAddress = await contracts.GetProfileAddressAsync(newLogin, cancellation.Token);
    var request = new OwnerChangeRequest(marketing, structure, oldAddress, newAddress, newLogin);
    var service = new OwnerChangeService(settings.ConnectionString);
    var preview = await service.RunAsync(request, false, cancellation.Token);
    Console.WriteLine("Program: {0}; structures: {1}", marketing, structure?.ToString() ?? "all > 0");
    Console.WriteLine("{0} ({1}) -> {2} ({3})", oldLogin, oldAddress, newLogin, newAddress);
    Console.WriteLine("Places: {0}; locks: {1}", preview.Places, preview.Locks);
    Console.WriteLine("New referral parent: {0} ({1})", preview.ParentLogin, preview.ParentAddress);
    if (preview.Places == 0 && preview.Locks == 0) return 0;
    Console.Write("Stop other Programs database writers. Type CHANGE to apply: ");
    if (Console.ReadLine()?.Trim() != "CHANGE") { Console.WriteLine("Cancelled; no changes made."); return 0; }
    var result = await service.RunAsync(request, true, cancellation.Token, preview);
    Console.WriteLine("Committed: {0} places, {1} locks; personal/referral volumes recalculated.", result.Places, result.Locks);
    return 0;
}
catch (OperationCanceledException) { Console.Error.WriteLine("Cancelled."); return 130; }
catch (OwnerChangeException exception) { Console.Error.WriteLine(exception.Message); return 1; }
catch (ArgumentException exception) { Console.Error.WriteLine(exception.Message); return 1; }
catch (Exception)
{
    // Npgsql errors can contain row data or connection details. Do not print them.
    Console.Error.WriteLine("Owner change failed. No partial changes committed. Check configuration, permissions, referral membership and place/lock identity conflicts.");
    return 1;
}

static string Required(string label)
{
    Console.Write(label + ": ");
    var value = Console.ReadLine();
    return string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException(label + " is required.") : value.Trim();
}
