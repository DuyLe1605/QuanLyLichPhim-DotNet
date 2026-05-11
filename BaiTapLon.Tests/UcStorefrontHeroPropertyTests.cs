// Feature: customer-ui-improvements, Property 10
using BaiTapLon.Forms.Customer;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace BaiTapLon.Tests;

/// <summary>
/// Property-based tests for UcStorefront hero text area width calculation.
/// </summary>
public class UcStorefrontHeroPropertyTests
{
    /// <summary>
    /// Property 10: Hero text area never overlaps the poster.
    /// Validates: Requirements 4.5
    ///
    /// For any hero panel width w >= 400:
    ///   - ComputeTextAreaWidth(w) > 0  (always positive)
    ///   - When w >= 470, ComputeTextAreaWidth(w) == w - 270  (no overlap)
    ///   - When w &lt; 470, ComputeTextAreaWidth(w) == 200  (minimum floor)
    ///
    /// Tag: // Feature: customer-ui-improvements, Property 10
    /// </summary>
    [Property]
    public Property HeroTextArea_IsAlwaysPositive_ForArbitraryWidth()
    {
        // Feature: customer-ui-improvements, Property 10
        // Generate widths >= 400
        var gen = Gen.Choose(400, 4000);

        return Prop.ForAll(gen.ToArbitrary(), width =>
        {
            float textWidth = UcStorefront.ComputeTextAreaWidth(width);

            // Must always be positive
            return textWidth > 0;
        });
    }

    [Property]
    public Property HeroTextArea_NeverOverlapsPoster_ForWideEnoughPanels()
    {
        // Feature: customer-ui-improvements, Property 10
        // For panels wide enough that posterLeft - 50 >= 200 (i.e., width >= 470),
        // the text area must not reach the poster region.
        // posterLeft = width - 220, text must stop 50px before poster: textWidth <= posterLeft - 50
        var gen = Gen.Choose(470, 4000);

        return Prop.ForAll(gen.ToArbitrary(), width =>
        {
            float textWidth = UcStorefront.ComputeTextAreaWidth(width);
            float posterLeft = width - 220;
            float maxSafe = posterLeft - 50f;

            // For wide panels, text area should equal posterLeft - 50 (no overlap)
            return textWidth <= maxSafe + 0.01f; // small epsilon for float comparison
        });
    }

    [Theory]
    [InlineData(400)]
    [InlineData(800)]
    [InlineData(1280)]
    [InlineData(1920)]
    public void ComputeTextAreaWidth_IsPositive(int width)
    {
        float result = UcStorefront.ComputeTextAreaWidth(width);
        Assert.True(result > 0, $"Expected positive width for panel width {width}, got {result}");
    }

    [Theory]
    [InlineData(800)]
    [InlineData(1280)]
    [InlineData(1920)]
    public void ComputeTextAreaWidth_DoesNotReachPoster_ForWidePanels(int width)
    {
        // Only test panels wide enough that the 200px floor doesn't apply
        float result = UcStorefront.ComputeTextAreaWidth(width);
        float posterLeft = width - 220;
        float maxSafe = posterLeft - 50f;

        Assert.True(result <= maxSafe,
            $"Text area width {result} overlaps poster at panel width {width}. " +
            $"Poster starts at {posterLeft}, max safe text width is {maxSafe}.");
    }

    [Fact]
    public void ComputeTextAreaWidth_UsesMinimumOf200_ForNarrowPanels()
    {
        // At width=400: posterLeft=180, posterLeft-50=130 < 200, so Math.Max(200, 130) = 200
        float result = UcStorefront.ComputeTextAreaWidth(400);
        Assert.Equal(200f, result);
    }

    [Fact]
    public void ComputeTextAreaWidth_ScalesWithWidth_ForWidePanels()
    {
        // At width=1280: posterLeft=1060, posterLeft-50=1010 > 200, so result=1010
        float result = UcStorefront.ComputeTextAreaWidth(1280);
        Assert.Equal(1010f, result);
    }

    [Fact]
    public void ComputeTextAreaWidth_At470_IsExactly200()
    {
        // At width=470: posterLeft=250, posterLeft-50=200 == 200, so Math.Max(200, 200) = 200
        float result = UcStorefront.ComputeTextAreaWidth(470);
        Assert.Equal(200f, result);
    }
}
