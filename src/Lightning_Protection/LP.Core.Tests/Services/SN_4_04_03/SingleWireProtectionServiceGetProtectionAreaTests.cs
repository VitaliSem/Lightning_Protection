using System;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.SingleWire;
using LP.Core.Services;
using LP.Core.Services.SN_4_04_03;

namespace LP.Core.Tests.Services.SN_4_04_03;

public class SingleWireProtectionServiceGetProtectionAreaTests
{
    private static SingleWireProtectionArea MakeArea(
        double x1, double y1, double r1,
        double x2, double y2, double r2) =>
        new SingleWireProtectionArea(new Point3D(x1, y1, 0), r1, new Point3D(x2, y2, 0), r2);

    // -------------------------------------------------------------------------
    // Input validation
    // -------------------------------------------------------------------------

    [Fact]
    public void GetProtectionArea_NullArea_ReturnsFail()
    {
        var result = SingleWireProtectionService.GetProtectionArea(null);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void GetProtectionArea_SameLocation_ReturnsFail()
    {
        var area = MakeArea(0, 0, 5, 0, 0, 3);

        var result = SingleWireProtectionService.GetProtectionArea(area);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void GetProtectionArea_OneCircleInsideOther_ReturnsFail()
    {
        // d=3, |r1-r2|=4 => circle 2 is inside circle 1
        var area = MakeArea(0, 0, 10, 3, 0, 6);

        var result = SingleWireProtectionService.GetProtectionArea(area);

        Assert.False(result.IsSuccess);
    }

    // -------------------------------------------------------------------------
    // Zero-radius degenerate cases
    // -------------------------------------------------------------------------

    [Fact]
    public void GetProtectionArea_Radius1Zero_ReturnsSinglePointWithRadius2()
    {
        var area = MakeArea(0, 0, 0, 10, 0, 5);

        var result = SingleWireProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Result.Points);
        Assert.Equal(5, result.Result.Points[0].Radius);
    }

    [Fact]
    public void GetProtectionArea_Radius2Zero_ReturnsSinglePointWithRadius1()
    {
        var area = MakeArea(0, 0, 5, 10, 0, 0);

        var result = SingleWireProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Result.Points);
        Assert.Equal(5, result.Result.Points[0].Radius);
    }

    // -------------------------------------------------------------------------
    // Success — result structure
    // -------------------------------------------------------------------------

    [Fact]
    public void GetProtectionArea_ValidInput_Returns4Points()
    {
        var area = MakeArea(0, 0, 5, 10, 0, 3);

        var result = SingleWireProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Result.Points.Count);
    }

    [Fact]
    public void GetProtectionArea_ValidInput_LastPointHasIsLastPointTrue()
    {
        var area = MakeArea(0, 0, 5, 10, 0, 3);

        var result = SingleWireProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.True(result.Result.Points[^1].IsLastPoint);
    }

    [Fact]
    public void GetProtectionArea_ValidInput_OnlyLastPointHasIsLastPointTrue()
    {
        var area = MakeArea(0, 0, 5, 10, 0, 3);

        var result = SingleWireProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        for (int i = 0; i < result.Result.Points.Count - 1; i++)
            Assert.False(result.Result.Points[i].IsLastPoint);
    }

    [Fact]
    public void GetProtectionArea_ValidInput_SecondPointHasRadius2()
    {
        var area = MakeArea(0, 0, 5, 10, 0, 3);

        var result = SingleWireProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Result.Points[1].Radius);
    }

    [Fact]
    public void GetProtectionArea_ValidInput_LastPointHasRadius1()
    {
        var area = MakeArea(0, 0, 5, 10, 0, 3);

        var result = SingleWireProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Result.Points[^1].Radius);
    }

    // -------------------------------------------------------------------------
    // Success — tangent point coordinates
    // -------------------------------------------------------------------------

    [Fact]
    public void GetProtectionArea_EqualRadii_TangentLinesAreParallelToLineOfCentres()
    {
        // Equal radii => α = arccos(0) = π/2 => tangent points are perpendicular to line of centres
        // c1=(0,0), c2=(10,0), r1=r2=4 => θ=0, α=π/2
        // upper tangent point on c1: (0+4*cos(π/2), 0+4*sin(π/2)) = (0, 4)
        // upper tangent point on c2: (10+4*cos(π/2), 0+4*sin(π/2)) = (10, 4)
        var area = MakeArea(0, 0, 4, 10, 0, 4);

        var result = SingleWireProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        var points = result.Result.Points;

        Assert.Equal(0, points[0].Point.X, precision: 5);
        Assert.Equal(4, points[0].Point.Y, precision: 5);

        Assert.Equal(10, points[1].Point.X, precision: 5);
        Assert.Equal(4, points[1].Point.Y, precision: 5);
    }

    [Fact]
    public void GetProtectionArea_EqualRadii_LowerTangentPointsAreSymmetric()
    {
        // lower tangent points should mirror upper ones about X axis
        var area = MakeArea(0, 0, 4, 10, 0, 4);

        var result = SingleWireProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        var points = result.Result.Points;

        // upper c2 y  == -lower c2 y
        Assert.Equal(points[1].Point.Y, -points[2].Point.Y, precision: 5);
        // upper c1 y  == -lower c1 y
        Assert.Equal(points[0].Point.Y, -points[3].Point.Y, precision: 5);
    }

    [Fact]
    public void GetProtectionArea_KnownValues_TangentPointsMatchExpected()
    {
        // c1=(0,0) r1=5, c2=(10,0) r2=3
        // d=10, θ=0, α=arccos((5-3)/10)=arccos(0.2)
        double r1 = 5, r2 = 3;
        double d = 10;
        double theta = 0;
        double alpha = Math.Acos((r1 - r2) / d);

        double xt1_upper = r1 * Math.Cos(theta + alpha);
        double yt1_upper = r1 * Math.Sin(theta + alpha);
        double xt2_upper = 10 + r2 * Math.Cos(theta + alpha);
        double yt2_upper = r2 * Math.Sin(theta + alpha);
        double xt2_lower = 10 + r2 * Math.Cos(theta - alpha);
        double yt2_lower = r2 * Math.Sin(theta - alpha);
        double xt1_lower = r1 * Math.Cos(theta - alpha);
        double yt1_lower = r1 * Math.Sin(theta - alpha);

        var area = MakeArea(0, 0, r1, 10, 0, r2);
        var result = SingleWireProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        var pts = result.Result.Points;

        Assert.Equal(xt1_upper, pts[0].Point.X, precision: 5);
        Assert.Equal(yt1_upper, pts[0].Point.Y, precision: 5);

        Assert.Equal(xt2_upper, pts[1].Point.X, precision: 5);
        Assert.Equal(yt2_upper, pts[1].Point.Y, precision: 5);

        Assert.Equal(xt2_lower, pts[2].Point.X, precision: 5);
        Assert.Equal(yt2_lower, pts[2].Point.Y, precision: 5);

        Assert.Equal(xt1_lower, pts[3].Point.X, precision: 5);
        Assert.Equal(yt1_lower, pts[3].Point.Y, precision: 5);
    }

    [Fact]
    public void GetProtectionArea_DiagonalLineOfCentres_TangentPointsUseCorrectTheta()
    {
        // c1=(0,0), c2=(6,8) => d=10, θ=atan2(8,6)
        double r1 = 5, r2 = 3;
        double d = 10;
        double theta = Math.Atan2(8, 6);
        double alpha = Math.Acos((r1 - r2) / d);

        double xt1_upper = r1 * Math.Cos(theta + alpha);
        double yt1_upper = r1 * Math.Sin(theta + alpha);

        var area = MakeArea(0, 0, r1, 6, 8, r2);
        var result = SingleWireProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.Equal(xt1_upper, result.Result.Points[0].Point.X, precision: 5);
        Assert.Equal(yt1_upper, result.Result.Points[0].Point.Y, precision: 5);
    }
}
