using System.Text.Json;
using IntegrationRequests;
using ReferalProgram.Infrastructure.IntegrationRequests;
using ScheduledTasks.Application;

namespace ReferalProgram.Application.Tests;

public sealed class DeactivateExpiredFirstPlacesCommandFactoryTests
{
    private static readonly DateTime Time = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("years")]
    [InlineData("months")]
    [InlineData("weeks")]
    [InlineData("days")]
    [InlineData("hours")]
    [InlineData("minutes")]
    public void Builds_request_with_period_target_and_stable_occurrence_time(string unit)
    {
        var factory = new ProgramTaskCommandRequestFactory();
        var command = Command(JsonSerializer.Serialize(new
        {
            structureNumber = 2, period = new { unit, value = 1 }
        }));
        var correlationId = Guid.NewGuid();
        Assert.True(factory.CanCreate(command));
        var request = Assert.IsType<DeactivateExpiredFirstPlacesRequest>(factory.Create(command, correlationId, Time));
        Assert.Equal("marketing", request.MarketingAddress);
        Assert.Equal(2, request.StructureNumber);
        Assert.Equal(new ActivityExpirationPeriod(unit, 1), request.Period);
        Assert.Equal(correlationId, request.CorrelationId);
        Assert.Equal(Time, request.OccurredOnUtc);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"structureNumber\":1}")]
    [InlineData("{\"structureNumber\":1,\"period\":null}")]
    [InlineData("{\"structureNumber\":1,\"period\":{\"unit\":\"days\",\"value\":0}}")]
    [InlineData("{\"structureNumber\":1,\"period\":{\"unit\":\"days\",\"value\":-1}}")]
    [InlineData("{\"structureNumber\":1,\"period\":{\"unit\":\"days\",\"value\":1.5}}")]
    [InlineData("{\"structureNumber\":1,\"period\":{\"unit\":\"seconds\",\"value\":1}}")]
    [InlineData("{\"structureNumber\":256,\"period\":{\"unit\":\"days\",\"value\":1}}")]
    [InlineData("{\"structureNumber\":\"1\",\"period\":{\"unit\":\"days\",\"value\":1}}")]
    public void Rejects_invalid_arguments_before_dispatch(string arguments)
    {
        Assert.Throws<FormatException>(() => new ProgramTaskCommandRequestFactory()
            .Create(Command(arguments), Guid.NewGuid(), Time));
    }

    [Fact]
    public void Old_broad_command_name_is_not_supported()
    {
        var command = Command("{}") with { Type = "program.structure.deactivate-expired-places" };
        Assert.False(new ProgramTaskCommandRequestFactory().CanCreate(command));
    }

    private static TaskCommandEnvelope Command(string arguments) => new(
        0, "program", DeactivateExpiredFirstPlacesRequest.CommandType, 1,
        JsonSerializer.SerializeToElement(new { marketingAddress = "marketing" }),
        JsonSerializer.Deserialize<JsonElement>(arguments));
}
