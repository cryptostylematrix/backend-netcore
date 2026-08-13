using System.Text.Json;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Services;

namespace ReferalProgram.Application.Tests;

public sealed class PositionConfigurationTests
{
    [Fact]
    public void Parser_applies_optional_algorithm_defaults()
    {
        using var document = JsonDocument.Parse("""
            {
              "v": 1,
              "root": "profile",
              "relation": "relative",
              "groups": [{ "id": 2, "algo": "future", "weight": 3 }]
            }
            """);

        var result = new PositionAlgorithmConfigurationParser().Parse(document.RootElement);

        var group = Assert.Single(result.Groups);
        Assert.Equal("future", group.Algorithm);
        Assert.True(group.ProfiledPlacesPrioritized);
        Assert.Equal((byte)1, group.DepthSpread);
    }

    [Fact]
    public void Parser_rejects_duplicate_group_ids()
    {
        using var document = JsonDocument.Parse("""
            {
              "v": 1,
              "root": "profile",
              "relation": "relative",
              "groups": [
                { "id": 1, "algo": "chess", "weight": 1 },
                { "id": 1, "algo": "radar", "weight": 1 }
              ]
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new PositionAlgorithmConfigurationParser().Parse(document.RootElement));

        Assert.Contains("unique", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Relative_selector_chooses_most_underrepresented_group()
    {
        var configuration = Configuration("relative", (0, 1), (1, 3));
        IReadOnlyDictionary<byte, long> counts = new Dictionary<byte, long>
        {
            [0] = 5,
            [1] = 5
        };

        var selected = new PositionGroupSelector().Select(configuration, counts);

        Assert.Equal(1, selected.Id);
    }

    [Fact]
    public void Absolute_selector_fills_current_weighted_round()
    {
        var configuration = Configuration("absolute", (0, 2), (1, 1));
        IReadOnlyDictionary<byte, long> counts = new Dictionary<byte, long>
        {
            [0] = 2,
            [1] = 0
        };

        var selected = new PositionGroupSelector().Select(configuration, counts);

        Assert.Equal(1, selected.Id);
    }

    private static PositionAlgorithmConfiguration Configuration(
        string relation,
        params (int Id, int Weight)[] groups) => new()
    {
        Version = 1,
        Root = "profile",
        Relation = relation,
        Groups = groups.Select(group => new PositionGroupConfiguration
        {
            Id = group.Id,
            Algorithm = "test",
            Weight = group.Weight
        }).ToArray()
    };
}
