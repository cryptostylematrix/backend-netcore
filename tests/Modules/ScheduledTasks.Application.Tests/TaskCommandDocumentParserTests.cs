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
}
