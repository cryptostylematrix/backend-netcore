namespace IntegrationRequests;

public sealed record ActivityExpirationPeriod(string Unit, int Value)
{
    public DateTime GetCutoffUtc(DateTime occurredOnUtc)
    {
        if (occurredOnUtc.Kind != DateTimeKind.Utc)
            throw new FormatException("The activity expiration reference time must be UTC.");
        if (Value <= 0)
            throw new FormatException("Activity expiration period.value must be a positive integer.");

        try
        {
            return Unit switch
            {
                "years" => occurredOnUtc.AddYears(-Value),
                "months" => occurredOnUtc.AddMonths(-Value),
                "weeks" => occurredOnUtc.AddDays(-7d * Value),
                "days" => occurredOnUtc.AddDays(-Value),
                "hours" => occurredOnUtc.AddHours(-Value),
                "minutes" => occurredOnUtc.AddMinutes(-Value),
                _ => throw new FormatException(
                    "Activity expiration period.unit must be years, months, weeks, days, hours or minutes.")
            };
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new FormatException("Activity expiration period exceeds the supported date range.", exception);
        }
    }
}
