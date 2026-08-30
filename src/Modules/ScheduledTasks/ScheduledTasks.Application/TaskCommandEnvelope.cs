using System.Text.Json;

namespace ScheduledTasks.Application;

public sealed record TaskCommandEnvelope(
    int Sequence,
    string Module,
    string Type,
    int Version,
    JsonElement Target,
    JsonElement Arguments);
