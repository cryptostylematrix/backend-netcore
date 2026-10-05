using System.Text.Json;
using System.Text.RegularExpressions;
using ReferalProgram.Application.Abstractions;
using ReferalProgram.Application.Policies;
using ReferalProgram.Core.PlaceAggregate;
using ReferalProgram.Core.ProfileVolumeAggregate;
using ReferalProgram.Dto;

namespace ReferalProgram.Application.Tests;

public sealed class CryptoCashActivityCompatibilityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Setup_configuration_preserves_activation_and_period_reset_cycle(bool useNewFormat)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "backend-netcore.sln")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var sql = File.ReadAllText(Path.Combine(directory.FullName,
            "src/Modules/ReferalProgram/Database/Scripts/setup_test_cryptocash_program.sql"));
        var match = Regex.Match(sql, @"v_structures jsonb :=\s*'(?<json>.*?)'::jsonb", RegexOptions.Singleline);
        Assert.True(match.Success);
        using var document = JsonDocument.Parse(match.Groups["json"].Value);
        var structures = document.RootElement.EnumerateArray().ToArray();
        Assert.Equal(5, structures.Length);
        Assert.False(structures[0].TryGetProperty("activity", out _));

        foreach (var item in structures.Skip(1))
        {
            var number = item.GetProperty("structure_number").GetByte();
            var activity = item.GetProperty("activity");
            Assert.True(activity.GetProperty("set_active_on_activation").GetBoolean());
            var structure = new StructureResponse
            {
                StructureNumber = number,
                Activity = useNewFormat
                    ? JsonSerializer.Deserialize<JsonElement>("{\"type\":\"marketing\"}")
                    : activity
            };
            var place = CreatePlace(number);
            var policy = new ActivatePlacePolicy(new Structures(), new Places(), new Commands());
            var tags = new HashSet<uint> { ProgramCommandTags.ActivatePlace };
            ActivatePlaceDecision Evaluate() => policy.Evaluate(structure, tags, new PlaceResponse
            {
                ProfileAddr = place.ProfileAddr,
                IsActive = place.IsActive,
                ActivatedAt = place.ActivatedAt
            });

            Assert.False(place.IsActive);
            var decision = Evaluate();
            Assert.True(decision.CanActivate);
            place.Activate(100, decision.SetActiveOnActivation);
            Assert.True(place.IsActive);
            Assert.Equal(100, place.ActivatedAt);
            Assert.Equal("place_already_activated", Evaluate().Reason);

            place.ResetActivity();
            Assert.True(place.IsActive);
            Assert.Null(place.ActivatedAt);
            decision = Evaluate();
            Assert.True(decision.CanActivate);
            place.Activate(200, decision.SetActiveOnActivation);
            place.ResetActivity();
            Assert.True(place.IsActive);
            Assert.Null(place.ActivatedAt);

            // A period without activation switches the place off; reset adds no volume.
            place.ResetActivity();
            Assert.False(place.IsActive);
            Assert.Null(place.ActivatedAt);
            Assert.Equal(2, place.DomainEvents.OfType<ProfileVolumeOperationDomainEvent>()
                .Count(e => e.Operation == ProfileVolumeOperation.ActivatePlace));
            Assert.True(Evaluate().CanActivate);
        }
    }

    private static Place CreatePlace(byte number) => Place.Create(
        parentId: 1, marketingAddr: "cryptocash", structureNumber: number,
        profileAddr: "profile", profileLogin: "profile", index: "profile1",
        placeNumber: 1, parentProfileAddr: "parent", parentProfileLogin: "parent",
        parentPlaceNumber: 1, mp: "0000000000000001", posGroup: 1,
        kind: PlaceKinds.Purchased, pos: 1, filling: 0, deep: 2,
        isActive: false, createdAt: 1, activatedAt: null);

    private sealed class Structures : IStructureQueries
    {
        public Task<StructureResponse?> GetStructureAsync(string marketingAddr, byte structureNumber,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Places : PlaceQueriesStub;

    private sealed class Commands : IProgramCommandQueries
    {
        public Task<ProgramCommandConfiguration> GetConfigurationAsync(string marketingAddr,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
