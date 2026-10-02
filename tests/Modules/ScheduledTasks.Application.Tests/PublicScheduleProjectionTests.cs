using System.Text.Json;
using IntegrationRequests.Scheduling;
using ScheduledTasks.Application.Abstractions;
using ScheduledTasks.Application;
using ScheduledTasks.Core.TaskAggregate;

namespace ScheduledTasks.Application.Tests;

public sealed class PublicScheduleProjectionTests
{
    private sealed class Descriptor : IPublicTaskCommandDescriptor
    {
        public string Module => "sample";
        public string BuildCommandFilter(string scope) => throw new NotSupportedException();
        public PublicScheduledAction? Describe(JsonElement command)
        {
            var type = command.GetProperty("type").GetString()!;
            if (command.GetProperty("module").GetString() != Module || !type.StartsWith("sample.item.")) return null;
            var id = command.GetProperty("arguments").GetProperty("item").GetInt32();
            if (id > 255) return null;
            return new(type, new(Module, command.GetProperty("target").GetProperty("scope").GetString()!, "item", id.ToString()));
        }
    }

    private static ScheduledTask Create(string commands, string? schedule = null) =>
        ScheduledTask.Create(Guid.NewGuid(), DateTimeOffset.Parse("2026-10-15T00:00:00Z"),
            schedule, commands, DateTimeOffset.Parse("2026-10-01T00:00:00Z"));

    [Fact]
    public void Projection_keeps_only_public_actions_for_the_requested_scope_in_order()
    {
        var row = Create("""
            [
              {"module":"sample","type":"sample.control.pause","target":{"scope":"selected"},"arguments":{}},
              {"module":"sample","type":"sample.item.refresh","target":{"scope":"other"},"arguments":{"item":1}},
              {"module":"sample","type":"sample.item.refresh","target":{"scope":"selected"},"arguments":{"item":2,"private":"secret"}},
              {"module":"sample","type":"sample.item.archive","target":{"scope":"selected"},"arguments":{"item":2}},
              {"module":"other","type":"sample.item.archive","target":{"scope":"selected"},"arguments":{"item":3}},
              {"module":"sample","type":"sample.item.archive","target":{"scope":"selected"},"arguments":{"item":256}}
            ]
            """, """{"type":"calendar","unit":"months","interval":3,"dayOfMonth":15,"timeUtc":"00:00:00"}""");

        var result = PublicScheduleProjection.Create(row, new ScheduleFilter("sample", "selected", "item"), new Descriptor());
        Assert.NotNull(result);
        Assert.Equal(["sample.item.refresh", "sample.item.archive"],
            result.Actions.Select(action => action.Type));
        Assert.All(result.Actions, action => Assert.Equal("2", action.Target.ResourceId));
        Assert.Equal(3, result.Interval);
        Assert.Equal(15, result.DayOfMonth);
        var json = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("secret", json);
        Assert.DoesNotContain("other", json);
        Assert.DoesNotContain("arguments", json);
        Assert.Contains("execute_at_utc", json);
    }

    [Fact]
    public void A_task_for_another_scope_is_not_returned()
    {
        var row = Create("""[{"module":"sample","type":"sample.item.archive","target":{"scope":"other"},"arguments":{"item":1}}]""");
        Assert.Null(PublicScheduleProjection.Create(row, new ScheduleFilter("sample", "selected", "item"), new Descriptor()));
    }

    [Theory]
    [InlineData("other", "selected", "item")]
    [InlineData("sample", "selected", "other")]
    public void Targets_outside_the_requested_module_or_resource_type_are_excluded(string module, string scope, string resourceType)
    {
        var row = Create("""[{"module":"sample","type":"sample.item.archive","target":{"scope":"selected"},"arguments":{"item":1}}]""");
        Assert.Null(PublicScheduleProjection.Create(row, new(module, scope, resourceType), new Descriptor()));
    }

    [Fact]
    public void Failure_status_is_public_but_internal_error_details_are_not()
    {
        var row = Create("""[{"module":"sample","type":"sample.item.archive","target":{"scope":"selected"},"arguments":{"item":1}}]""");
        row.MarkFailed("private connection details", DateTimeOffset.Parse("2026-10-15T00:01:00Z"));
        var result = PublicScheduleProjection.Create(row, new ScheduleFilter("sample", "selected", "item"), new Descriptor());
        Assert.NotNull(result);
        Assert.Equal("error", result.Status);
        Assert.DoesNotContain("private connection details", JsonSerializer.Serialize(result));
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("{\"type\":\"interval\",\"unit\":\"days\",\"value\":2}", "interval", 2)]
    public void One_time_and_interval_schedules_preserve_their_meaning(string? schedule, string? type, int? interval)
    {
        var row = Create("""[{"module":"sample","type":"sample.item.archive","target":{"scope":"selected"},"arguments":{"item":1}}]""", schedule);
        var result = PublicScheduleProjection.Create(row, new ScheduleFilter("sample", "selected", "item"), new Descriptor());
        Assert.NotNull(result);
        Assert.Equal(type, result.ScheduleType);
        Assert.Equal(interval, result.Interval);
        Assert.Equal("active", result.Status);
        Assert.Equal(row.ExecuteAtUtc, result.ExecuteAtUtc);
    }
}
