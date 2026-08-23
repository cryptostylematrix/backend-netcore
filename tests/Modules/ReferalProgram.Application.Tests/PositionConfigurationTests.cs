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
    public void Version_two_uses_default_when_operation_is_omitted_or_has_no_override()
    {
        using var document = JsonDocument.Parse("""
            {
              "v": 2,
              "default": {
                "root": "owner",
                "relation": "relative",
                "groups": [{ "id": 0, "algo": "chess", "weight": 1 }]
              },
              "operations": {
                "buy_first_place": {
                  "root": "profile",
                  "relation": "relative",
                  "groups": [{ "id": 0, "algo": "classic", "weight": 1 }]
                }
              }
            }
            """);
        var parser = new PositionAlgorithmConfigurationParser();

        var withoutOperation = parser.Parse(document.RootElement);
        var withoutOverride = parser.Parse(
            document.RootElement,
            PositionOperation.CreateClone);

        Assert.Equal("owner", withoutOperation.Root);
        Assert.Equal("chess", Assert.Single(withoutOperation.Groups).Algorithm);
        Assert.Equal("owner", withoutOverride.Root);
        Assert.Equal("chess", Assert.Single(withoutOverride.Groups).Algorithm);
    }

    [Fact]
    public void Version_two_uses_the_requested_operation_override()
    {
        using var document = JsonDocument.Parse("""
            {
              "v": 2,
              "default": {
                "root": "owner",
                "relation": "relative",
                "groups": [{ "id": 0, "algo": "chess", "weight": 1 }]
              },
              "operations": {
                "buy_first_place": {
                  "root": "profile",
                  "relation": "absolute",
                  "groups": [{ "id": 2, "algo": "classic", "weight": 3 }]
                }
              }
            }
            """);

        var result = new PositionAlgorithmConfigurationParser().Parse(
            document.RootElement,
            PositionOperation.BuyFirstPlace);

        Assert.Equal(2, result.Version);
        Assert.Equal("profile", result.Root);
        Assert.Equal("absolute", result.Relation);
        var group = Assert.Single(result.Groups);
        Assert.Equal(2, group.Id);
        Assert.Equal("classic", group.Algorithm);
    }

    [Fact]
    public void Version_two_requires_a_default_configuration()
    {
        using var document = JsonDocument.Parse("""
            { "v": 2, "operations": {} }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new PositionAlgorithmConfigurationParser().Parse(document.RootElement));

        Assert.Contains("default", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parser_reads_trimmed_classic_cut_factor()
    {
        using var document = JsonDocument.Parse("""
            {
              "v": 1,
              "root": "profile",
              "relation": "relative",
              "groups": [
                {
                  "id": 0,
                  "algo": "trimmed_classic",
                  "weight": 1,
                  "cut_factor": 3
                }
              ]
            }
            """);

        var result = new PositionAlgorithmConfigurationParser().Parse(
            document.RootElement);

        Assert.Equal((uint)3, Assert.Single(result.Groups).CutFactor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Parser_rejects_trimmed_classic_cut_factor_below_two(int cutFactor)
    {
        using var document = JsonDocument.Parse($$"""
            {
              "v": 1,
              "root": "profile",
              "relation": "relative",
              "groups": [
                {
                  "id": 0,
                  "algo": "trimmed_classic",
                  "weight": 1,
                  "cut_factor": {{cutFactor}}
                }
              ]
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new PositionAlgorithmConfigurationParser().Parse(document.RootElement));

        Assert.Contains("at least 2", exception.Message);
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
