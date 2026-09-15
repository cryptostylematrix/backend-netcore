namespace ReferalProgram.Application.Tests;

public sealed class ProfileFrontierEdgeCaseTests
{
    [Theory]
    [InlineData(1, new[] { 1 })]
    [InlineData(4, new[] { 1, 4, 2, 3 })]
    [InlineData(5, new[] { 1, 5, 2, 4, 3 })]
    public void Chess_order_alternates_from_both_edges(
        int width,
        int[] expected)
    {
        var actual = Enumerable.Range(1, width)
            .OrderBy(index => ChessRank(index, width))
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Binary_level_expands_to_configured_width_without_broken_chains()
    {
        var parents = Parents(count: 16);

        var targetWidth = FillLevel(parents, configuredLimit: 32, structureWidth: 2);

        Assert.Equal(32, targetWidth);
        Assert.All(parents, parent => Assert.Equal(2, parent.ProfiledChildren));
    }

    [Fact]
    public void Non_power_of_two_limit_continues_every_chain_before_widening()
    {
        var parents = Parents(count: 32);

        var targetWidth = FillLevel(parents, configuredLimit: 35, structureWidth: 2);

        Assert.Equal(35, targetWidth);
        Assert.All(parents, parent => Assert.True(parent.ProfiledChildren >= 1));
        Assert.Equal(new[] { 1, 2, 32 }, parents
            .Where(parent => parent.ProfiledChildren == 2)
            .Select(parent => parent.HorizontalIndex)
            .ToArray());
    }

    [Fact]
    public void Historical_level_wider_than_limit_becomes_a_pipe()
    {
        var parents = Parents(count: 72);

        var targetWidth = FillLevel(parents, configuredLimit: 32, structureWidth: 2);

        Assert.Equal(72, targetWidth);
        Assert.All(parents, parent => Assert.Equal(1, parent.ProfiledChildren));
    }

    [Fact]
    public void Historical_imbalance_expands_only_for_broken_chains()
    {
        var parents = new[]
        {
            new ParentState(1, ProfiledChildren: 2, Filling: 2),
            new ParentState(2, ProfiledChildren: 0, Filling: 0),
            new ParentState(3, ProfiledChildren: 1, Filling: 1)
        };

        var targetWidth = FillLevel(
            parents,
            configuredLimit: 3,
            structureWidth: 2,
            initialTargetWidth: 3);

        Assert.Equal(4, targetWidth);
        Assert.Equal(new[] { 2, 1, 1 }, parents.Select(x => x.ProfiledChildren));
    }

    [Fact]
    public void Locked_minimum_chain_cannot_be_overtaken()
    {
        var parents = new[]
        {
            new ParentState(1, ProfiledChildren: 1, Filling: 1),
            new ParentState(2, ProfiledChildren: 0, Filling: 0, Locked: true),
            new ParentState(3, ProfiledChildren: 1, Filling: 1),
            new ParentState(4, ProfiledChildren: 1, Filling: 1)
        };

        Assert.Null(SelectParent(parents, configuredLimit: 8, structureWidth: 2, targetWidth: 3));

        parents[1].Locked = false;
        Assert.Same(
            parents[1],
            SelectParent(parents, configuredLimit: 8, structureWidth: 2, targetWidth: 3));
    }

    [Fact]
    public void System_filling_preserves_the_last_slot_for_profile_continuation()
    {
        var parent = new ParentState(
            HorizontalIndex: 1,
            ProfiledChildren: 0,
            Filling: 1);

        Assert.Same(
            parent,
            SelectParent(new[] { parent }, configuredLimit: 1, structureWidth: 2, targetWidth: 1));

        parent.Filling = 2;
        Assert.Null(
            SelectParent(new[] { parent }, configuredLimit: 1, structureWidth: 2, targetWidth: 1));
    }

    [Fact]
    public void Stable_chess_rank_does_not_shift_after_leftmost_parent_is_filled()
    {
        var parents = Parents(count: 4);
        parents[0].ProfiledChildren = 1;
        parents[0].Filling = 1;

        var selected = SelectParent(
            parents,
            configuredLimit: 8,
            structureWidth: 2,
            targetWidth: 1);

        Assert.Equal(4, selected?.HorizontalIndex);
    }

    [Fact]
    public void Width_and_balance_invariants_hold_across_boundary_grid()
    {
        for (var currentWidth = 1; currentWidth <= 64; currentWidth++)
        {
            for (var configuredLimit = 1; configuredLimit <= 128; configuredLimit++)
            {
                for (var structureWidth = 1; structureWidth <= 4; structureWidth++)
                {
                    var parents = Parents(currentWidth);
                    var targetWidth = FillLevel(
                        parents,
                        configuredLimit,
                        structureWidth);
                    var expectedWidth = currentWidth >= configuredLimit
                        ? currentWidth
                        : Math.Min(configuredLimit, currentWidth * structureWidth);

                    Assert.Equal(expectedWidth, targetWidth);
                    Assert.True(
                        parents.Max(parent => parent.ProfiledChildren)
                        - parents.Min(parent => parent.ProfiledChildren) <= 1);

                    if (expectedWidth >= currentWidth)
                    {
                        Assert.All(
                            parents,
                            parent => Assert.True(parent.ProfiledChildren >= 1));
                    }

                    if (currentWidth >= configuredLimit)
                    {
                        Assert.All(
                            parents,
                            parent => Assert.Equal(1, parent.ProfiledChildren));
                    }
                }
            }
        }
    }

    [Fact]
    public void Historical_repairs_add_exactly_one_place_per_broken_chain()
    {
        for (var currentWidth = 1; currentWidth <= 64; currentWidth++)
        {
            var parents = Enumerable.Range(1, currentWidth)
                .Select(index => new ParentState(
                    index,
                    ProfiledChildren: index % 3,
                    Filling: index % 3))
                .ToArray();
            var initialTargetWidth = parents.Sum(parent => parent.ProfiledChildren);
            var brokenChains = parents.Count(parent => parent.ProfiledChildren == 0);

            var targetWidth = FillLevel(
                parents,
                configuredLimit: currentWidth,
                structureWidth: 3,
                initialTargetWidth);

            Assert.Equal(initialTargetWidth + brokenChains, targetWidth);
            Assert.All(parents, parent => Assert.True(parent.ProfiledChildren >= 1));
        }
    }

    private static ParentState[] Parents(int count) => Enumerable.Range(1, count)
        .Select(index => new ParentState(index, ProfiledChildren: 0, Filling: 0))
        .ToArray();

    private static int FillLevel(
        IReadOnlyList<ParentState> parents,
        int configuredLimit,
        int structureWidth,
        int initialTargetWidth = 0)
    {
        var targetWidth = initialTargetWidth;
        while (SelectParent(parents, configuredLimit, structureWidth, targetWidth) is { } parent)
        {
            parent.ProfiledChildren++;
            parent.Filling++;
            targetWidth++;
        }

        return targetWidth;
    }

    private static ParentState? SelectParent(
        IReadOnlyList<ParentState> parents,
        int configuredLimit,
        int structureWidth,
        int targetWidth)
    {
        var currentWidth = parents.Count;
        var effectiveWidth = Math.Max(configuredLimit, currentWidth);
        var continuable = parents
            .Where(parent => parent.Active
                && parent.Kind != 2
                && (structureWidth == 0 || parent.Filling < structureWidth))
            .ToArray();
        if (continuable.Length == 0)
            return null;

        var minimumChildren = continuable.Min(parent => parent.ProfiledChildren);

        return continuable
            .Where(parent => parent.ProfiledChildren == minimumChildren)
            .Where(parent => targetWidth < effectiveWidth || parent.ProfiledChildren == 0)
            .Where(parent => currentWidth < configuredLimit || parent.ProfiledChildren == 0)
            .Where(parent => !parent.Locked)
            .OrderBy(parent => ChessRank(parent.HorizontalIndex, currentWidth))
            .FirstOrDefault();
    }

    private static int ChessRank(int horizontalIndex, int horizontalCount) =>
        horizontalIndex <= (horizontalCount + 1) / 2
            ? horizontalIndex * 2 - 1
            : (horizontalCount - horizontalIndex + 1) * 2;

    private sealed class ParentState(
        int HorizontalIndex,
        int ProfiledChildren,
        int Filling,
        bool Active = true,
        int Kind = 0,
        bool Locked = false)
    {
        public int HorizontalIndex { get; } = HorizontalIndex;
        public int ProfiledChildren { get; set; } = ProfiledChildren;
        public int Filling { get; set; } = Filling;
        public bool Active { get; } = Active;
        public int Kind { get; } = Kind;
        public bool Locked { get; set; } = Locked;
    }
}
