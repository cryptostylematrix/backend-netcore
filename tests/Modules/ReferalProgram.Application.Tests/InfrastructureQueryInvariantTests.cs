namespace ReferalProgram.Application.Tests;

public sealed class InfrastructureQueryInvariantTests
{
    [Fact]
    public void Every_open_candidate_query_excludes_terminal_clones()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(
            root,
            "src",
            "Modules",
            "ReferalProgram",
            "ReferalProgram.Infrastructure",
            "Queries",
            "PlaceQueries.cs");
        var source = File.ReadAllText(path);

        Assert.Equal(3, Count(source, "AND kind <> 2"));
        AssertMethodContains(source,
            "GetUnfilledPlacesInDepthWindowAsync",
            "AND kind <> 2");
        AssertMethodContains(source,
            "GetFirstActiveUnfilledPlaceAsync",
            "AND kind <> 2");
        AssertMethodContains(source,
            "GetOpenPlacesByMpPrefixAsync",
            "AND kind <> 2");
    }

    private static void AssertMethodContains(
        string source,
        string methodName,
        string expected)
    {
        var methodStart = source.IndexOf(methodName, StringComparison.Ordinal);
        Assert.True(methodStart >= 0, $"Method {methodName} was not found.");

        var nextMethod = source.IndexOf(
            "public async Task",
            methodStart + methodName.Length,
            StringComparison.Ordinal);
        var method = nextMethod < 0
            ? source[methodStart..]
            : source[methodStart..nextMethod];

        Assert.Contains(expected, method, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend-netcore.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static int Count(string value, string fragment) =>
        value.Split(fragment, StringSplitOptions.None).Length - 1;
}
