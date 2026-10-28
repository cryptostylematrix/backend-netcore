using ProgramPlaceParentRepairer;

try
{
    if (args.Any(argument => argument is not "--apply"))
        throw new ArgumentException("Supported option: --apply. Without it, the tool only checks data.");
    var connectionString = AppSettings.LoadConnectionString();
    using var cancellation = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
    Console.Write("Marketing contract address (empty = all registered programs): ");
    var marketing = Console.ReadLine()?.Trim();
    Console.Write("Structure (0–255; empty = all structures in the database): ");
    var input = Console.ReadLine();
    short? structure = null;
    if (!string.IsNullOrWhiteSpace(input))
    {
        if (!short.TryParse(input, out var number) || number is < 0 or > 255)
            throw new ArgumentException("Structure must be between 0 and 255.");
        structure = number;
    }
    var service = new ParentRepairService(connectionString);
    void ShowProgress(ParentRepairProgress progress) => Console.WriteLine(
        "{0}: [{1}/{2}] {3} / structure {4}; checked={5}, mismatched={6}",
        progress.Phase, progress.Completed, progress.Total, progress.Target.Marketing,
        progress.Target.Structure, progress.Checked, progress.Mismatches);
    Console.WriteLine("Programs: {0}; structures: {1}",
        string.IsNullOrWhiteSpace(marketing) ? "all registered" : marketing,
        structure?.ToString() ?? "all in the database");
    var preview = await service.RunAsync(marketing, structure, false, cancellation.Token, progress: ShowProgress);
    Console.WriteLine("Selected: {0} programs, {1} structures", preview.Targets.Select(target => target.Marketing).Distinct().Count(), preview.Targets.Count);
    if (preview.Targets.Count == 0) { Console.WriteLine("No matching structures found; no changes made."); return 0; }
    Console.WriteLine("Checked: {0}; mismatched: {1}; invalid parent references: {2}",
        preview.Checked, preview.Mismatches.Count, preview.InvalidParents);
    foreach (var row in preview.Mismatches.Take(50))
    {
        Console.WriteLine("Program={0}, place id={1}, structure={2}, parent_id={3}{4}",
            row.Marketing, row.Id, row.Structure, row.ParentId?.ToString() ?? "null", row.InvalidParent ? " [INVALID PARENT]" : "");
        Console.WriteLine("  Current:  addr={0}, login={1}, number={2}",
            row.Current.Address ?? "null", row.Current.Login ?? "null", row.Current.Number?.ToString() ?? "null");
        Console.WriteLine("  Expected: addr={0}, login={1}, number={2}",
            row.Expected.Address ?? "null", row.Expected.Login ?? "null", row.Expected.Number?.ToString() ?? "null");
    }
    if (preview.Mismatches.Count > 50) Console.WriteLine("Only the first 50 mismatches are displayed.");
    if (preview.InvalidParents > 0)
        throw new ParentRepairException("Invalid physical parent references found. Repair is blocked; no changes made.");
    if (!args.Contains("--apply"))
    {
        Console.WriteLine("Check complete; no changes made. Run with --apply to repair.");
        return 0;
    }
    if (preview.Mismatches.Count == 0) { Console.WriteLine("No repair needed."); return 0; }
    Console.Write("Stop other Programs database writers. Type REPAIR to apply: ");
    if (Console.ReadLine()?.Trim() != "REPAIR") { Console.WriteLine("Cancelled; no changes made."); return 0; }
    var result = await service.RunAsync(marketing, structure, true, cancellation.Token, preview, ShowProgress);
    Console.WriteLine("Committed: repaired parent fields on {0} places; verification passed.", result.Mismatches.Count);
    return 0;
}
catch (OperationCanceledException) { Console.Error.WriteLine("Cancelled."); return 130; }
catch (ParentRepairException exception) { Console.Error.WriteLine(exception.Message); return 1; }
catch (ArgumentException exception) { Console.Error.WriteLine(exception.Message); return 1; }
catch (Exception)
{
    Console.Error.WriteLine("Parent repair failed. Check database configuration and SELECT/UPDATE permissions. No partial repair is committed.");
    return 1;
}
