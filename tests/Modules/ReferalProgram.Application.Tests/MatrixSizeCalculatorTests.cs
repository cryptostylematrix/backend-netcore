using ReferalProgram.Application.Services;

namespace ReferalProgram.Application.Tests;

public sealed class MatrixSizeCalculatorTests
{
    [Theory]
    [InlineData(2, 2, 7)]
    [InlineData(2, 1, 3)]
    [InlineData(1, 5, 6)]
    [InlineData(0, 5, 1)]
    [InlineData(5, 0, 1)]
    public void Calculates_place_and_all_matrix_levels(
        byte width,
        byte height,
        long expected)
    {
        Assert.Equal(expected, MatrixSizeCalculator.Calculate(width, height));
    }

    [Fact]
    public void Rejects_matrix_sizes_that_do_not_fit_the_response_type()
    {
        Assert.Throws<OverflowException>(() =>
            MatrixSizeCalculator.Calculate(byte.MaxValue, byte.MaxValue));
    }

    [Theory]
    [InlineData(2, 2, 6, 6)]
    [InlineData(0, 2, 6, 1)]
    [InlineData(2, 0, 6, 1)]
    public void Resolves_persisted_filling_only_for_matrix_structures(
        byte width,
        byte height,
        long persisted,
        long expected)
    {
        Assert.Equal(
            expected,
            MatrixSizeCalculator.ResolveFilling(width, height, persisted));
    }
}
