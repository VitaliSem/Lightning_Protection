using System;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.DoubleRod;
using LP.Core.Models.LightningProtectors.SingleRod;
using LP.Core.Services;
using LP.Core.Services.SN_4_04_03;

namespace LP.Core.Tests.Services.SN_4_04_03;

public class DoubleRodProtectionServiceGetProtectionAreaTests
{
    private static DoubleRodProtectionArea MakeNotCombinedArea(
        double x1, double y1, double r1,
        double x2, double y2, double r2) =>
        new DoubleRodProtectionArea(
            new SingleRodProtectionArea(new Point3D(x1, y1, 0), r1),
            new SingleRodProtectionArea(new Point3D(x2, y2, 0), r2),
            h0: 10, r0: 10, l: 50, lmax: 30, lc: 20,
            hc: 10, isCombined: false,
            rx: 0, lx: 0, rcx: 0);

    /// <summary>Creates a combined area for the hx >= hc case (rcx = 0).
    /// rx1 = rx2 = rx — symmetric rods.</summary>
    private static DoubleRodProtectionArea MakeCombinedAreaHxGeHc(
        double x1, double y1,
        double x2, double y2,
        double rx, double lx, double L) =>
        new DoubleRodProtectionArea(
            new SingleRodProtectionArea(new Point3D(x1, y1, 0), rx),
            new SingleRodProtectionArea(new Point3D(x2, y2, 0), rx),
            h0: 10, r0: 10, l: L, lmax: 50, lc: 20,
            hc: 8, isCombined: true,
            rx: rx, lx: lx, rcx: 0);

    /// <summary>Creates a combined area for the hx &lt; hc case (rcx &gt; 0).
    /// rx1 = rx2 = rx — symmetric rods, midpoints have radius rcx.</summary>
    private static DoubleRodProtectionArea MakeCombinedAreaHxLtHc(
        double x1, double y1,
        double x2, double y2,
        double rx, double lx, double rcx, double L) =>
        new DoubleRodProtectionArea(
            new SingleRodProtectionArea(new Point3D(x1, y1, 0), rx),
            new SingleRodProtectionArea(new Point3D(x2, y2, 0), rx),
            h0: 10, r0: 10, l: L, lmax: 50, lc: 20,
            hc: 8, isCombined: true,
            rx: rx, lx: lx, rcx: rcx);

    // -------------------------------------------------------------------------
    // Input validation
    // -------------------------------------------------------------------------

    [Fact]
    public void GetProtectionArea_NullArea_ReturnsFail()
    {
        var result = DoubleRodProtectionService.GetProtectionArea(null);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Message);
    }

    // -------------------------------------------------------------------------
    // Not combined
    // -------------------------------------------------------------------------

    [Fact]
    public void GetProtectionArea_NotCombined_Returns2Points()
    {
        var area = MakeNotCombinedArea(0, 0, 5, 10, 0, 3);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Result.Points.Count);
    }

    [Fact]
    public void GetProtectionArea_NotCombined_LastPointHasIsLastPointTrue()
    {
        var area = MakeNotCombinedArea(0, 0, 5, 10, 0, 3);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.True(result.Result.Points[^1].IsLastPoint);
    }

    [Fact]
    public void GetProtectionArea_NotCombined_OnlyLastPointHasIsLastPoint()
    {
        var area = MakeNotCombinedArea(0, 0, 5, 10, 0, 3);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        for (int i = 0; i < result.Result.Points.Count - 1; i++)
            Assert.False(result.Result.Points[i].IsLastPoint);
    }

    [Fact]
    public void GetProtectionArea_NotCombined_FirstPointHasRadiusR1()
    {
        var area = MakeNotCombinedArea(0, 0, 5, 10, 0, 3);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Result.Points[0].Radius);
    }

    [Fact]
    public void GetProtectionArea_NotCombined_SecondPointHasRadiusR2()
    {
        var area = MakeNotCombinedArea(0, 0, 5, 10, 0, 3);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Result.Points[1].Radius);
    }

    // -------------------------------------------------------------------------
    // Combined, hx >= hc (rcx = 0) — result structure
    // Point order (radius on START of arc):
    //   [0] rod1 upper (rx1) → arc → [1] rod1 lower → line → [2] mid lower
    //   → line → [3] rod2 lower (rx2) → arc → [4] rod2 upper → line → [5] mid upper (last)
    // -------------------------------------------------------------------------

    [Fact]
    public void GetProtectionArea_CombinedHxGeHc_Returns6Points()
    {
        var area = MakeCombinedAreaHxGeHc(0, 0, 10, 0, rx: 5, lx: 3, L: 10);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Result.Points.Count);
    }

    [Fact]
    public void GetProtectionArea_CombinedHxGeHc_LastPointHasIsLastPointTrue()
    {
        var area = MakeCombinedAreaHxGeHc(0, 0, 10, 0, rx: 5, lx: 3, L: 10);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.True(result.Result.Points[^1].IsLastPoint);
    }

    [Fact]
    public void GetProtectionArea_CombinedHxGeHc_OnlyLastPointHasIsLastPoint()
    {
        var area = MakeCombinedAreaHxGeHc(0, 0, 10, 0, rx: 5, lx: 3, L: 10);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        for (int i = 0; i < result.Result.Points.Count - 1; i++)
            Assert.False(result.Result.Points[i].IsLastPoint);
    }

    [Fact]
    public void GetProtectionArea_CombinedHxGeHc_OnlyArcStartPointsHaveRadius()
    {
        // Use different radii for each rod to verify asymmetric case
        var area = new DoubleRodProtectionArea(
            new SingleRodProtectionArea(new Point3D(0, 0, 0), 5),
            new SingleRodProtectionArea(new Point3D(10, 0, 0), 3),
            h0: 10, r0: 10, l: 10, lmax: 50, lc: 20,
            hc: 8, isCombined: true,
            rx: 5, lx: 3, rcx: 0, lx1: 2, lx2: 3);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        var pts = result.Result.Points;

        // [0] rod1 upper — start of arc for rod1 (rx1)
        Assert.Equal(5, pts[0].Radius);
        // [1] rod1 lower — end of arc for rod1 (no radius)
        Assert.Null(pts[1].Radius);
        // [2] mid lower — line vertex (no radius)
        Assert.Null(pts[2].Radius);
        // [3] rod2 lower — start of arc for rod2 (rx2)
        Assert.Equal(3, pts[3].Radius);
        // [4] rod2 upper — end of arc for rod2 (no radius)
        Assert.Null(pts[4].Radius);
        // [5] mid upper — line vertex (no radius)
        Assert.Null(pts[5].Radius);
    }

    // -------------------------------------------------------------------------
    // Combined, hx >= hc — coordinates
    // -------------------------------------------------------------------------

    [Fact]
    public void GetProtectionArea_CombinedHxGeHc_HorizontalCenters_CoordinatesMatchExpected()
    {
        // c1=(0,0), c2=(10,0), rx=5, lx=3
        // theta=0, perp = (-sin0, cos0) = (0, 1)
        double rx = 5, lx = 3;
        var area = new DoubleRodProtectionArea(
            new SingleRodProtectionArea(new Point3D(0, 0, 0), rx),
            new SingleRodProtectionArea(new Point3D(10, 0, 0), rx),
            h0: 10, r0: 10, l: 10, lmax: 50, lc: 20,
            hc: 8, isCombined: true,
            rx: rx, lx: lx, rcx: 0, lx1: lx, lx2: lx);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        var pts = result.Result.Points;

        // [0] rod1 upper
        Assert.Equal(0, pts[0].Point.X, precision: 5);
        Assert.Equal(rx, pts[0].Point.Y, precision: 5);

        // [1] rod1 lower
        Assert.Equal(0, pts[1].Point.X, precision: 5);
        Assert.Equal(-rx, pts[1].Point.Y, precision: 5);

        // [2] mid lower
        Assert.Equal(0, pts[2].Point.X, precision: 5);
        Assert.Equal(-lx, pts[2].Point.Y, precision: 5);

        // [3] rod2 lower
        Assert.Equal(10, pts[3].Point.X, precision: 5);
        Assert.Equal(-rx, pts[3].Point.Y, precision: 5);

        // [4] rod2 upper
        Assert.Equal(10, pts[4].Point.X, precision: 5);
        Assert.Equal(rx, pts[4].Point.Y, precision: 5);

        // [5] mid upper
        Assert.Equal(0, pts[5].Point.X, precision: 5);
        Assert.Equal(lx, pts[5].Point.Y, precision: 5);
    }

    [Fact]
    public void GetProtectionArea_CombinedHxGeHc_DiagonalCenters_PointsRotatedCorrectly()
    {
        // c1=(0,0), c2=(6,8) => d=10, theta=atan2(8,6)
        // perp = (-sin(theta), cos(theta))
        double rx = 5, lx = 3;
        double theta = Math.Atan2(8, 6);
        double perpX = -Math.Sin(theta);
        double perpY = Math.Cos(theta);

        // Upper point on c1
        double exP0x = 0 + perpX * rx;
        double exP0y = 0 + perpY * rx;

        // Lower point on c1
        double exP1x = 0 - perpX * rx;
        double exP1y = 0 - perpY * rx;

        var area = new DoubleRodProtectionArea(
            new SingleRodProtectionArea(new Point3D(0, 0, 0), rx),
            new SingleRodProtectionArea(new Point3D(6, 8, 0), rx),
            h0: 10, r0: 10, l: 10, lmax: 50, lc: 20,
            hc: 8, isCombined: true,
            rx: rx, lx: lx, rcx: 0, lx1: lx, lx2: lx);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        var pts = result.Result.Points;

        // [0] rod1 upper
        Assert.Equal(exP0x, pts[0].Point.X, precision: 5);
        Assert.Equal(exP0y, pts[0].Point.Y, precision: 5);

        // [1] rod1 lower
        Assert.Equal(exP1x, pts[1].Point.X, precision: 5);
        Assert.Equal(exP1y, pts[1].Point.Y, precision: 5);
    }

    [Fact]
    public void GetProtectionArea_CombinedHxGeHc_SymmetricAboutCentreLine()
    {
        double rx = 5, lx = 3;
        var area = new DoubleRodProtectionArea(
            new SingleRodProtectionArea(new Point3D(0, 0, 0), rx),
            new SingleRodProtectionArea(new Point3D(10, 0, 0), rx),
            h0: 10, r0: 10, l: 10, lmax: 50, lc: 20,
            hc: 8, isCombined: true,
            rx: rx, lx: lx, rcx: 0, lx1: lx, lx2: lx);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        var pts = result.Result.Points;

        // [0] rod1 upper y == -[1] rod1 lower y, same x
        Assert.Equal(pts[0].Point.X, pts[1].Point.X, precision: 5);
        Assert.Equal(pts[0].Point.Y, -pts[1].Point.Y, precision: 5);

        // [3] rod2 lower y == -[4] rod2 upper y, same x
        Assert.Equal(pts[3].Point.X, pts[4].Point.X, precision: 5);
        Assert.Equal(pts[3].Point.Y, -pts[4].Point.Y, precision: 5);

        // [2] mid lower y == -[5] mid upper y
        Assert.Equal(pts[2].Point.X, pts[5].Point.X, precision: 5);
        Assert.Equal(pts[2].Point.Y, -pts[5].Point.Y, precision: 5);
    }

    // -------------------------------------------------------------------------
    // Combined, hx < hc (rcx > 0) — result structure
    // Point order (radius on START of arc):
    //   [0] rod1 upper (rx) → arc → [1] mid upper (rcx) → arc → [2] rod2 upper (rx)
    //   → arc → [3] rod2 lower (rx) → arc → [4] mid lower (rcx) → arc → [5] rod1 lower (rx, last)
    // -------------------------------------------------------------------------

    [Fact]
    public void GetProtectionArea_CombinedHxLtHc_Returns6Points()
    {
        var area = MakeCombinedAreaHxLtHc(0, 0, 10, 0, rx: 5, lx: 3, rcx: 2, L: 10);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Result.Points.Count);
    }

    [Fact]
    public void GetProtectionArea_CombinedHxLtHc_LastPointHasIsLastPointTrue()
    {
        var area = MakeCombinedAreaHxLtHc(0, 0, 10, 0, rx: 5, lx: 3, rcx: 2, L: 10);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        Assert.True(result.Result.Points[^1].IsLastPoint);
    }

    [Fact]
    public void GetProtectionArea_CombinedHxLtHc_AllArcStartPointsHaveRadius()
    {
        var area = MakeCombinedAreaHxLtHc(0, 0, 10, 0, rx: 5, lx: 3, rcx: 2, L: 10);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        // Every point starts an arc segment to the next point
        Assert.Equal(5, result.Result.Points[0].Radius);  // rod1 upper → mid upper
        Assert.Equal(2, result.Result.Points[1].Radius);  // mid upper → rod2 upper
        Assert.Equal(5, result.Result.Points[2].Radius);  // rod2 upper → rod2 lower
        Assert.Equal(5, result.Result.Points[3].Radius);  // rod2 lower → mid lower
        Assert.Equal(2, result.Result.Points[4].Radius);  // mid lower → rod1 lower
        Assert.Equal(5, result.Result.Points[5].Radius);  // rod1 lower → rod1 upper (closing)
    }

    [Fact]
    public void GetProtectionArea_CombinedHxLtHc_HorizontalCenters_CoordinatesMatchExpected()
    {
        // c1=(0,0), c2=(10,0), rx=5, lx=3
        // theta=0, perp = (-sin0, cos0) = (0, 1)
        double rx = 5, lx = 3, rcx = 2;
        var area = MakeCombinedAreaHxLtHc(0, 0, 10, 0, rx: rx, lx: lx, rcx: rcx, L: 10);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        var pts = result.Result.Points;

        // [0] rod1 upper
        Assert.Equal(0, pts[0].Point.X, precision: 5);
        Assert.Equal(rx, pts[0].Point.Y, precision: 5);

        // [1] mid upper
        Assert.Equal(5, pts[1].Point.X, precision: 5);
        Assert.Equal(lx, pts[1].Point.Y, precision: 5);

        // [2] rod2 upper
        Assert.Equal(10, pts[2].Point.X, precision: 5);
        Assert.Equal(rx, pts[2].Point.Y, precision: 5);

        // [3] rod2 lower
        Assert.Equal(10, pts[3].Point.X, precision: 5);
        Assert.Equal(-rx, pts[3].Point.Y, precision: 5);

        // [4] mid lower
        Assert.Equal(5, pts[4].Point.X, precision: 5);
        Assert.Equal(-lx, pts[4].Point.Y, precision: 5);

        // [5] rod1 lower
        Assert.Equal(0, pts[5].Point.X, precision: 5);
        Assert.Equal(-rx, pts[5].Point.Y, precision: 5);
    }

    [Fact]
    public void GetProtectionArea_CombinedHxLtHc_SymmetricAboutCentreLine()
    {
        var area = MakeCombinedAreaHxLtHc(0, 0, 10, 0, rx: 5, lx: 3, rcx: 2, L: 10);

        var result = DoubleRodProtectionService.GetProtectionArea(area);

        Assert.True(result.IsSuccess);
        var pts = result.Result.Points;

        // [0] rod1 upper y == -[5] rod1 lower y, same x
        Assert.Equal(pts[0].Point.X, pts[5].Point.X, precision: 5);
        Assert.Equal(pts[0].Point.Y, -pts[5].Point.Y, precision: 5);

        // [1] mid upper y == -[4] mid lower y, same x
        Assert.Equal(pts[1].Point.X, pts[4].Point.X, precision: 5);
        Assert.Equal(pts[1].Point.Y, -pts[4].Point.Y, precision: 5);

        // [2] rod2 upper y == -[3] rod2 lower y, same x
        Assert.Equal(pts[2].Point.X, pts[3].Point.X, precision: 5);
        Assert.Equal(pts[2].Point.Y, -pts[3].Point.Y, precision: 5);
    }
}