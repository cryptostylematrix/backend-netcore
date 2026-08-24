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

    [Fact]
    public void Get_places_filters_closed_matrices_using_persisted_filling()
    {
        var source = ReadPlaceQueries();

        AssertMethodContains(source,
            "GetPlacesAsync",
            "matrix_filling < @matrixSize");
        AssertMethodContains(source,
            "GetPlacesAsync",
            "OR NOT @isMatrixStructure");
        AssertMethodContains(source,
            "GetPlacesAsync",
            "WHEN @isMatrixStructure THEN matrix_filling");
    }

    [Fact]
    public void Matrix_filling_updates_use_bounded_parent_traversal()
    {
        var root = FindRepositoryRoot();
        var repository = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Modules",
            "ReferalProgram",
            "ReferalProgram.Infrastructure",
            "Repositories",
            "PlaceRepository.cs"));
        var recalculator = File.ReadAllText(Path.Combine(
            root,
            "src",
            "ProgramMatrixFillingRecalculator",
            "MatrixFillingRecalculator.cs"));

        AssertMethodContains(
            repository,
            "IncrementMatrixFillingForAncestorsAsync",
            "CROSS JOIN LATERAL generate_series");
        AssertMethodContains(
            repository,
            "IncrementMatrixFillingForAncestorsAsync",
            "matrix_filling = ancestor.matrix_filling + 1");
        AssertMethodContains(
            repository,
            "IncrementMatrixFillingForAncestorsAsync",
            "JOIN public.structures structure");
        AssertMethodContains(
            repository,
            "IncrementMatrixFillingForAncestorsAsync",
            "structure_config.width > 0");
        AssertMethodContains(
            repository,
            "IncrementMatrixFillingForAncestorsAsync",
            "FOR SHARE OF structure");
        AssertMethodContains(
            repository,
            "IncrementMatrixFillingForAncestorsAsync",
            "structure_config.deep - structure_config.height + 1");
        AssertMethodContains(
            repository,
            "IncrementMatrixFillingForAncestorsAsync",
            "ancestor.deep = ancestor_level.deep");
        Assert.DoesNotContain("WITH RECURSIVE", repository, StringComparison.Ordinal);
        Assert.Contains("ancestors.distance < @height", recalculator, StringComparison.Ordinal);
        Assert.DoesNotContain("LIKE", recalculator, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Get_tree_loads_persisted_filling_and_descendants_in_one_batch()
    {
        var source = ReadPlaceQueries();

        AssertMethodContains(source,
            "GetTreeCountsByMpAsync",
            "FROM unnest(@mpPrefixes::text[])");
        AssertMethodContains(source,
            "GetTreeCountsByMpAsync",
            "roots.matrix_filling");
        AssertMethodContains(source,
            "GetTreeCountsByMpAsync",
            "COUNT(descendants.descendant_id) - 1");
        AssertMethodContains(source,
            "GetTreeCountsByMpAsync",
            "child.parent_id = descendants.descendant_id");
        Assert.DoesNotContain("GetPlaceSubtreeCountsAsync", source, StringComparison.Ordinal);
    }

    private static string ReadPlaceQueries()
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

        return File.ReadAllText(path);
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
