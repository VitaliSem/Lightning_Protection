using LP.Core.Constants;
using LP.Core.Models.Base;
using LP.Core.Services;

namespace LP.Core.Tests.Services;

public class ViewFocusServiceTests
{
    private const double Tolerance = 1e-9;

    private static ViewFocusDto MakeDto(double minX, double minY, double maxX, double maxY, double aspectRatio) =>
        new ViewFocusDto
        {
            Min = new Point3D(minX, minY),
            Max = new Point3D(maxX, maxY),
            AspectRatio = aspectRatio,
        };

    // -------------------------------------------------------------------------
    // Valid extents
    // -------------------------------------------------------------------------

    [Fact]
    public void CalculateFocusWindow_Center_IsExtentsMidpoint()
    {
        var result = ViewFocusService.CalculateFocusWindow(MakeDto(10, 20, 14, 30, 1.5));

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Result.CenterX, Tolerance);
        Assert.Equal(25, result.Result.CenterY, Tolerance);
    }

    [Fact]
    public void CalculateFocusWindow_SquareExtents_SquareView_FillsFraction()
    {
        var result = ViewFocusService.CalculateFocusWindow(MakeDto(0, 0, 4, 4, 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(4 / ViewFocusConstants.FillFraction, result.Result.Height, Tolerance);
        Assert.Equal(4 / ViewFocusConstants.FillFraction, result.Result.Width, Tolerance);
    }

    [Fact]
    public void CalculateFocusWindow_WideExtents_NarrowView_WidthIsLimiting()
    {
        // extents 10 x 2 in a view twice as tall as wide: width governs
        var result = ViewFocusService.CalculateFocusWindow(MakeDto(0, 0, 10, 2, 0.5));

        Assert.True(result.IsSuccess);
        Assert.Equal(10 / ViewFocusConstants.FillFraction, result.Result.Width, Tolerance);
        Assert.Equal(20 / ViewFocusConstants.FillFraction, result.Result.Height, Tolerance);
    }

    [Fact]
    public void CalculateFocusWindow_TallExtents_WideView_HeightIsLimiting()
    {
        var result = ViewFocusService.CalculateFocusWindow(MakeDto(0, 0, 2, 10, 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(10 / ViewFocusConstants.FillFraction, result.Result.Height, Tolerance);
        Assert.Equal(20 / ViewFocusConstants.FillFraction, result.Result.Width, Tolerance);
    }

    [Theory]
    [InlineData(0, 0, 4, 4, 1.0)]
    [InlineData(0, 0, 10, 2, 0.5)]
    [InlineData(-5, -1, 5, 3, 1.777)]
    [InlineData(3, 3, 3, 3, 2.0)]
    public void CalculateFocusWindow_Width_IsHeightTimesAspectRatio(double minX, double minY, double maxX, double maxY, double aspect)
    {
        var result = ViewFocusService.CalculateFocusWindow(MakeDto(minX, minY, maxX, maxY, aspect));

        Assert.True(result.IsSuccess);
        Assert.Equal(result.Result.Height * aspect, result.Result.Width, Tolerance);
    }

    [Fact]
    public void CalculateFocusWindow_ZeroSizeExtents_UsesMinViewHeight()
    {
        var result = ViewFocusService.CalculateFocusWindow(MakeDto(7, 8, 7, 8, 1.5));

        Assert.True(result.IsSuccess);
        Assert.Equal(ViewFocusConstants.MinViewHeight, result.Result.Height, Tolerance);
        Assert.Equal(ViewFocusConstants.MinViewHeight * 1.5, result.Result.Width, Tolerance);
        Assert.Equal(7, result.Result.CenterX, Tolerance);
        Assert.Equal(8, result.Result.CenterY, Tolerance);
    }

    // -------------------------------------------------------------------------
    // Invalid input
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CalculateFocusWindow_InvalidAspectRatio_Fails(double aspect)
    {
        var result = ViewFocusService.CalculateFocusWindow(MakeDto(0, 0, 1, 1, aspect));

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }

    [Theory]
    [InlineData(5, 0, 1, 1)]
    [InlineData(0, 5, 1, 1)]
    public void CalculateFocusWindow_InvertedExtents_Fails(double minX, double minY, double maxX, double maxY)
    {
        var result = ViewFocusService.CalculateFocusWindow(MakeDto(minX, minY, maxX, maxY, 1));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void CalculateFocusWindow_NaNCoordinate_Fails()
    {
        var result = ViewFocusService.CalculateFocusWindow(MakeDto(double.NaN, 0, 1, 1, 1));

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void CalculateFocusWindow_NullDto_Fails()
    {
        var result = ViewFocusService.CalculateFocusWindow(null!);

        Assert.False(result.IsSuccess);
    }
}
