using IntegrationRequests;

namespace ReferalProgram.Application.Tests;

public sealed class ActivityExpirationPeriodTests
{
    [Theory]
    [InlineData("years", 1, "2024-03-31T12:00:00Z", "2023-03-31T12:00:00Z")]
    [InlineData("years", 1, "2024-02-29T12:00:00Z", "2023-02-28T12:00:00Z")]
    [InlineData("months", 1, "2024-03-31T12:00:00Z", "2024-02-29T12:00:00Z")]
    [InlineData("months", 2, "2025-03-31T12:00:00Z", "2025-01-31T12:00:00Z")]
    [InlineData("weeks", 2, "2024-03-31T12:00:00Z", "2024-03-17T12:00:00Z")]
    [InlineData("days", 1, "2024-03-31T12:00:00Z", "2024-03-30T12:00:00Z")]
    [InlineData("hours", 3, "2024-03-31T12:00:00Z", "2024-03-31T09:00:00Z")]
    [InlineData("minutes", 30, "2024-03-31T12:00:00Z", "2024-03-31T11:30:00Z")]
    public void Calculates_calendar_or_fixed_utc_cutoff(string unit, int value, string time, string expected)
    {
        var result = new ActivityExpirationPeriod(unit, value)
            .GetCutoffUtc(DateTimeOffset.Parse(time).UtcDateTime);
        Assert.Equal(DateTimeOffset.Parse(expected).UtcDateTime, result);
    }

    [Theory]
    [InlineData("days", 0)]
    [InlineData("days", -1)]
    [InlineData("seconds", 1)]
    [InlineData("unknown", 1)]
    [InlineData("years", int.MaxValue)]
    [InlineData("months", int.MaxValue)]
    [InlineData("weeks", int.MaxValue)]
    public void Rejects_invalid_or_overflowing_periods(string unit, int value)
    {
        Assert.Throws<FormatException>(() => new ActivityExpirationPeriod(unit, value)
            .GetCutoffUtc(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc)));
    }
}
