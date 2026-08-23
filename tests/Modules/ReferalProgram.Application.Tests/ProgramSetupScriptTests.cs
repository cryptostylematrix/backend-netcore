using System.Text.RegularExpressions;

namespace ReferalProgram.Application.Tests;

public sealed class ProgramSetupScriptTests
{
    public static TheoryData<string, string[]> LoopBasedPrograms => new()
    {
        {
            "setup_lifestyler_program.sql",
            [
                "FOR v_structure_number IN 0..11",
                "CASE WHEN v_structure_number = 0 THEN 1 ELSE 0 END, CASE WHEN v_structure_number = 0 THEN 0 ELSE 2 END, CASE WHEN v_structure_number = 0 THEN 1 ELSE 2 END, CASE WHEN v_structure_number = 0 THEN 1 ELSE 2 END, v_structure_number BETWEEN 2 AND 11, v_pos_algo"
            ]
        },
        {
            "setup_silver_matrix_program.sql",
            [
                "FOR v_structure_number IN 0..2",
                "CASE WHEN v_structure_number = 0 THEN 1 ELSE 0 END, CASE WHEN v_structure_number = 0 THEN 0 ELSE 2 END, CASE WHEN v_structure_number = 0 THEN 1 ELSE 2 END, CASE WHEN v_structure_number = 0 THEN 1 ELSE 2 END, v_structure_number = 2, v_pos_algo"
            ]
        },
        {
            "setup_gold_matrix_program.sql",
            [
                "FOR v_structure_number IN 0..4",
                "CASE WHEN v_structure_number = 0 THEN 1 ELSE 0 END, CASE WHEN v_structure_number = 0 THEN 0 ELSE 2 END, CASE WHEN v_structure_number = 0 THEN 1 ELSE 2 END, CASE WHEN v_structure_number = 0 THEN 1 ELSE 2 END, v_structure_number BETWEEN 2 AND 4, v_pos_algo"
            ]
        },
        {
            "setup_elite_program.sql",
            [
                "FOR v_structure_number IN 0..7",
                "WHEN v_structure_number = 0 THEN 0 WHEN v_structure_number = 7 THEN 2 ELSE 4 END, 1, 1, v_structure_number BETWEEN 3 AND 7, v_pos_algo"
            ]
        },
        {
            "setup_mini_program.sql",
            [
                "FOR v_structure_number IN 0..17",
                "CASE WHEN v_structure_number = 0 THEN 1 ELSE 0 END, CASE WHEN v_structure_number = 0 THEN 0 ELSE 2 END, 1, 1, v_structure_number IN (2, 3, 5, 7, 9, 11, 13, 15, 17), v_pos_algo",
                "'create_clone', v_trimmed_classic_config, 'create_reinvest', v_trimmed_classic_config",
                "'algo', 'trimmed_classic'",
                "'cut_factor', v_cut_factor",
                "IF v_cut_factor < 2"
            ]
        },
        {
            "setup_super_matrix_program.sql",
            [
                "FOR v_structure_number IN 0..4",
                "CASE WHEN v_structure_number = 0 THEN 1 ELSE 0 END, CASE WHEN v_structure_number = 0 THEN 0 ELSE 3 END, 1, 1, v_structure_number BETWEEN 2 AND 4, v_pos_algo"
            ]
        },
        {
            "setup_flash_matrix_program.sql",
            [
                "FOR v_structure_number IN 0..8",
                "CASE WHEN v_structure_number = 0 THEN 1 ELSE 0 END, CASE WHEN v_structure_number = 0 THEN 0 ELSE 2 END, 1, 1, v_structure_number BETWEEN 2 AND 8, v_pos_algo"
            ]
        }
    };

    [Theory]
    [MemberData(nameof(LoopBasedPrograms))]
    public void Production_setup_preserves_confirmed_topology(
        string scriptName,
        string[] expectedFragments)
    {
        var sql = ReadNormalized(scriptName);

        Assert.True(
            sql.Contains("'root', 'profile'", StringComparison.Ordinal)
            || sql.Contains("\"root\": \"profile\"", StringComparison.Ordinal),
            $"{scriptName} must use a profile-root position configuration.");
        foreach (var fragment in expectedFragments)
            Assert.Contains(Normalize(fragment), sql, StringComparison.Ordinal);
    }

    [Fact]
    public void CryptoCash_setup_preserves_owner_root_chess_and_radar_topology()
    {
        var sql = ReadNormalized("setup_test_cryptocash_program.sql");

        Assert.Equal(5, Count(sql, "\"structure_number\":"));
        Assert.Equal(5, Count(sql, "\"max_places_per_profile\": 1"));
        Assert.Equal(4, Count(sql, "\"height\": 0"));
        Assert.Equal(4, Count(sql, "\"root\": \"owner\""));
        Assert.Equal(4, Count(sql, "\"algo\": \"chess\""));
        Assert.Equal(4, Count(sql, "\"algo\": \"radar\""));
        Assert.Equal(4, Count(sql, "\"depth_spread\": 3"));
    }

    [Fact]
    public void Multi_setup_preserves_seven_profile_root_classic_structures()
    {
        var sql = ReadNormalized("setup_test_multi_program.sql");

        Assert.Equal(7, Count(sql, "\"structure_number\":"));
        Assert.Equal(6, Count(sql, "\"width\": 2"));
        Assert.Equal(6, Count(sql, "\"height\": 2"));
        Assert.Equal(5, Count(sql, "\"prev_required\": true"));
        Assert.Equal(7, Count(sql, "\"root\":\"profile\""));
        Assert.Equal(7, Count(sql, "\"algo\":\"classic\""));
    }

    [Fact]
    public void Neo_setup_preserves_line_and_matrix_widths()
    {
        var sql = ReadNormalized("setup_test_neo_program.sql");

        Assert.Equal(9, Count(sql, "\"structure_number\":"));
        Assert.Equal(2, Count(sql, "\"width\": 0"));
        Assert.Equal(6, Count(sql, "\"width\": 4"));
        Assert.Equal(1, Count(sql, "\"width\": 3"));
        Assert.Contains("v_structure.width, 1, 1, false, v_pos_algo", sql,
            StringComparison.Ordinal);
        Assert.Contains("\"root\": \"profile\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"algo\": \"classic\"", sql, StringComparison.Ordinal);
    }

    private static string ReadNormalized(string scriptName)
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(
            root,
            "src",
            "Modules",
            "ReferalProgram",
            "Database",
            "Scripts",
            scriptName);

        Assert.True(File.Exists(path), $"Setup script was not found: {path}");
        return Normalize(File.ReadAllText(path));
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

    private static string Normalize(string value) =>
        Regex.Replace(value, @"\s+", " ").Trim();

    private static int Count(string value, string fragment) =>
        value.Split(fragment, StringSplitOptions.None).Length - 1;
}
