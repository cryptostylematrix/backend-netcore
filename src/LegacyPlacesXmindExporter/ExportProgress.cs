using System.Diagnostics;
using System.Globalization;

namespace LegacyPlacesXmindExporter;

// A timer also reports activity while PostgreSQL or disk IO is waiting.
public sealed class ExportProgress : IDisposable
{
    private readonly TextWriter output;
    private readonly Timer timer;
    private readonly object gate = new();
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
    private readonly Stopwatch stageElapsed = new();
    private string? stage;
    private long completed;
    private long? total;
    private bool disposed;

    public ExportProgress(TextWriter output)
    {
        this.output = output;
        timer = new Timer(_ => Report(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    public void Start(string name, long? count = null)
    {
        lock (gate)
        {
            stage = name;
            total = count;
            Interlocked.Exchange(ref completed, 0);
            stageElapsed.Restart();
            Write("начало");
        }
    }

    public void Advance() => Interlocked.Increment(ref completed);

    public void Complete()
    {
        lock (gate)
        {
            Write("завершено");
            stage = null;
        }
    }

    private void Report()
    {
        lock (gate)
        {
            if (!disposed && stage is not null)
                Write("в работе");
        }
    }

    private void Write(string status)
    {
        var count = Interlocked.Read(ref completed);
        var amount = count.ToString("N0", CultureInfo.InvariantCulture);
        if (total is { } expected)
            amount += $" / {expected.ToString("N0", CultureInfo.InvariantCulture)} ({(expected == 0 ? 100 : 100.0 * count / expected):F1}%)";
        var rate = count / Math.Max(stageElapsed.Elapsed.TotalSeconds, 0.001);
        output.WriteLine($"[{elapsed.Elapsed:hh\\:mm\\:ss}] {stage}: {amount}; {rate:F0} мест/с; {status}");
        output.Flush();
    }

    public void Dispose()
    {
        lock (gate)
            disposed = true;
        timer.Dispose();
    }
}
