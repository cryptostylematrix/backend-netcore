namespace ReferalProgram.Application.Services;

internal static class MatrixSizeCalculator
{
    public static long Calculate(byte width, byte height)
    {
        if (width == 0 || height == 0)
            return 1;

        long matrixSize = 1;
        long placesAtLevel = 1;

        for (var level = 1; level <= height; level++)
        {
            placesAtLevel = checked(placesAtLevel * width);
            matrixSize = checked(matrixSize + placesAtLevel);
        }

        return matrixSize;
    }

    public static long ResolveFilling(byte width, byte height, long persistedFilling) =>
        width > 0 && height > 0 ? persistedFilling : 1;
}
