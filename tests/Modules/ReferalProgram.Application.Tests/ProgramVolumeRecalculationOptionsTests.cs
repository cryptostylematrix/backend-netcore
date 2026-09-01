using ProgramVolumeRecalculator;

namespace ReferalProgram.Application.Tests;

public sealed class ProgramVolumeRecalculationOptionsTests
{
    [Theory]
    [InlineData("personal", "Personal")]
    [InlineData("referral", "Referral")]
    [InlineData("group", "Group")]
    public void Parses_all_supported_volume_types(string value, string expected)
    {
        var options = RecalculationOptions.Parse(
        [
            "--connection-string", "Host=localhost;Database=test",
            "--marketing-addr", "marketing",
            "--structure-number", "0",
            "--type", value,
            "--apply"
        ]);

        Assert.Equal(expected, options.Type.ToString());
        Assert.Equal((byte)0, options.StructureNumber);
        Assert.True(options.ApplyChanges);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("256")]
    public void Rejects_structure_number_outside_byte_range(string value)
    {
        Assert.Throws<OptionsException>(() => RecalculationOptions.Parse(
        [
            "--connection-string", "Host=localhost;Database=test",
            "--marketing-addr", "marketing",
            "--structure-number", value,
            "--type", "referral"
        ]));
    }
}
