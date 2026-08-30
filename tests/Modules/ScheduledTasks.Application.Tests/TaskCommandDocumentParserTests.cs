using ScheduledTasks.Application;

namespace ScheduledTasks.Application.Tests;

public sealed class TaskCommandDocumentParserTests
{
    private readonly TaskCommandDocumentParser _parser = new();

    [Fact]
    public void Parses_ordered_program_target()
    {
        var commands = _parser.Parse(
            """
            [{
              "module":"program",
              "type":"program.structure.compress",
              "version":1,
              "target":{"marketingAddress":"EQ_TEST"},
              "arguments":{"structureNumber":3}
            }]
            """);

        var command = Assert.Single(commands);
        Assert.Equal(0, command.Sequence);
        Assert.Equal("program", command.Module);
        Assert.Equal(
            "EQ_TEST",
            command.Target.GetProperty("marketingAddress").GetString());
        Assert.Equal(3, command.Arguments.GetProperty("structureNumber").GetInt32());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("[{}]")]
    public void Rejects_invalid_documents(string json)
    {
        Assert.Throws<FormatException>(() => _parser.Parse(json));
    }

    [Fact]
    public void Extracts_known_marketing_even_when_another_command_is_malformed()
    {
        var addresses = _parser.GetProgramMarketingAddresses(
            """
            [
              {
                "module":"program",
                "type":"program.structure.compress",
                "target":{"marketingAddress":"EQ_BLOCKED"}
              },
              {}
            ]
            """);

        Assert.Equal(["EQ_BLOCKED"], addresses);
    }
}
