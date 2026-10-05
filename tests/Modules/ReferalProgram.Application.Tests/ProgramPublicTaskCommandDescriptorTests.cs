using System.Text.Json;
using ReferalProgram.Application.Features.Structures;
using Xunit;

namespace ReferalProgram.Application.Tests;

public sealed class ProgramPublicTaskCommandDescriptorTests
{
    [Theory]
    [InlineData("program.structure.update-activity")]
    [InlineData("program.structure.deactivate-expired-first-places")]
    [InlineData("program.structure.compress")]
    [InlineData("program.structure.calculate-referral-volume")]
    [InlineData("program.structure.reset-referral-volume")]
    public void Public_structure_actions_expose_only_safe_target(string type)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(new {
            module = "program", type, version = 1, target = new { marketingAddress = "selected", secret = "hidden" },
            arguments = new { structureNumber = 0, secret = "hidden" }
        }));
        var result = new ProgramPublicTaskCommandDescriptor().Describe(json.RootElement);
        Assert.NotNull(result);
        Assert.Equal(type, result.Type);
        Assert.Equal("program", result.Target.Module);
        Assert.Equal("selected", result.Target.Scope);
        Assert.Equal("structure", result.Target.ResourceType);
        Assert.Equal("0", result.Target.ResourceId);
        Assert.DoesNotContain("hidden", JsonSerializer.Serialize(result));
    }

    [Theory]
    [InlineData("program", "program.task-processing.disable", 1, 1)]
    [InlineData("program", "program.structure.deactivate-expired-places", 1, 1)]
    [InlineData("other", "program.structure.compress", 1, 1)]
    [InlineData("program", "program.structure.compress", -1, 1)]
    [InlineData("program", "program.structure.compress", 256, 1)]
    [InlineData("program", "program.structure.compress", 1, 2)]
    public void Private_foreign_or_unsupported_commands_are_not_public(string module, string type, int structure, int version)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(new {
            module, type, version, target = new { marketingAddress = "selected" }, arguments = new { structureNumber = structure }
        }));
        Assert.Null(new ProgramPublicTaskCommandDescriptor().Describe(json.RootElement));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"module\":\"program\",\"type\":\"program.structure.compress\",\"target\":null}")]
    public void Incomplete_commands_are_not_public(string command)
    {
        using var json = JsonDocument.Parse(command);
        Assert.Null(new ProgramPublicTaskCommandDescriptor().Describe(json.RootElement));
    }

    [Fact]
    public void Database_filter_contains_module_and_escaped_scope()
    {
        const string scope = "address\"with quote";
        using var json = JsonDocument.Parse(new ProgramPublicTaskCommandDescriptor().BuildCommandFilter(scope));
        Assert.Equal("program", json.RootElement[0].GetProperty("module").GetString());
        Assert.Equal(scope, json.RootElement[0].GetProperty("target").GetProperty("marketingAddress").GetString());
    }
}
