namespace ProgramMigrator;

internal interface IMigrationProgress
{
    void Stage(string message);
    void Report(string operation, int completed, int total);
}

internal sealed class ConsoleMigrationProgress : IMigrationProgress
{
    public void Stage(string message)
    {
        Console.WriteLine();
        Console.WriteLine(message);
    }

    public void Report(string operation, int completed, int total)
    {
        if (total <= 0)
        {
            Console.WriteLine("{0}: 0/0", operation);
            return;
        }

        var reportingInterval = Math.Max(1, total / 20);
        if (completed != 1
            && completed != total
            && completed % reportingInterval != 0)
        {
            return;
        }

        var percentage = completed * 100d / total;
        Console.WriteLine(
            "{0}: {1}/{2} ({3:F1}%)",
            operation,
            completed,
            total,
            percentage);
    }
}
