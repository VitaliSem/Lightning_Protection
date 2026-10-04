using System;
using LP.Core.Enums;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.DoubleRod;
using LP.Core.Models.LightningProtectors.SingleRod;
using LP.Core.Services;
using LP.Core.Services.SN_4_04_03;

namespace LP.Core.Tests.Services.SN_4_04_03;

public class DoubleRodProtectionServiceTests
{
    private static DoubleRodLightningProtector MakeProtector(double h1, double h2, double distance = 50) =>
        new DoubleRodLightningProtector(
            new SingleRodLightningProtector(new Point3D(0, 0, 0), h1),
            new SingleRodLightningProtector(new Point3D(distance, 0, 0), h2));

    private static DoubleRodLightningProtector MakeSymmetricProtector(double h, double distance = 50) =>
        MakeProtector(h, h, distance);

    // -------------------------------------------------------------------------
    // Input validation
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_NullProtector_ReturnsFail()
    {
        var result = DoubleRodProtectionService.Calculate(null, LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void Calculate_Rod1Null_ReturnsFail()
    {
        var protector = new DoubleRodLightningProtector(null, new SingleRodLightningProtector(new Point3D(50, 0, 0), 30));

        var result = DoubleRodProtectionService.Calculate(protector, LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_Rod2Null_ReturnsFail()
    {
        var protector = new DoubleRodLightningProtector(new SingleRodLightningProtector(new Point3D(0, 0, 0), 30), null);

        var result = DoubleRodProtectionService.Calculate(protector, LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_Height1Zero_ReturnsFail()
    {
        var result = DoubleRodProtectionService.Calculate(MakeProtector(0, 30), LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_Height2Zero_ReturnsFail()
    {
        var result = DoubleRodProtectionService.Calculate(MakeProtector(30, 0), LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_Height1Negative_ReturnsFail()
    {
        var result = DoubleRodProtectionService.Calculate(MakeProtector(-5, 30), LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_Height2Exceeds150_ReturnsFail()
    {
        var result = DoubleRodProtectionService.Calculate(MakeProtector(30, 151), LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_HxNegative_ReturnsFail()
    {
        var result = DoubleRodProtectionService.Calculate(MakeSymmetricProtector(30), LightningProtectionReliability.P0_900, -1);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_HxEqualToMinHeight_ReturnsFail()
    {
        // h1=30, h2=50, hx=30 => fails since hx equals the shorter rod height
        var result = DoubleRodProtectionService.Calculate(MakeProtector(30, 50), LightningProtectionReliability.P0_900, 30);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_HxGreaterThanMinHeight_ReturnsFail()
    {
        var result = DoubleRodProtectionService.Calculate(MakeProtector(30, 50), LightningProtectionReliability.P0_900, 35);

        Assert.False(result.IsSuccess);
    }

    // -------------------------------------------------------------------------
    // Step 1: Cone parameters
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_EqualHeights_H0MatchesSingleRod()
    {
        // h1=h2=30, P0_900: h0=0.85*30=25.5
        var result = DoubleRodProtectionService.Calculate(MakeSymmetricProtector(30), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.85 * 30, result.Result.H0, precision: 5);
    }

    [Fact]
    public void Calculate_UnequalHeights_H0IsAveraged()
    {
        // h1=30 (h0=0.85*30=25.5), h2=60 (h0=0.85*60=51), P0_900
        double expectedH0 = (0.85 * 30 + 0.85 * 60) / 2.0;

        var result = DoubleRodProtectionService.Calculate(MakeProtector(30, 60), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedH0, result.Result.H0, precision: 5);
    }

    [Fact]
    public void Calculate_UnequalHeights_R0IsAveraged()
    {
        // h1=30 (r0=1.2*30=36), h2=60 (r0=1.2*60=72), P0_900
        double expectedR0 = (1.2 * 30 + 1.2 * 60) / 2.0;

        var result = DoubleRodProtectionService.Calculate(MakeProtector(30, 60), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedR0, result.Result.R0, precision: 5);
    }

    // -------------------------------------------------------------------------
    // Step 2: Geometry
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_Ldistance_MatchesGet2DDistance()
    {
        double distance = 50;

        var result = DoubleRodProtectionService.Calculate(MakeSymmetricProtector(30, distance), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(distance, result.Result.L, precision: 5);
    }

    [Fact]
    public void Calculate_Ldistance_DiagonalPositions()
    {
        // rods at (0,0) and (30,40) => d = sqrt(30^2 + 40^2) = 50
        var protector = new DoubleRodLightningProtector(
            new SingleRodLightningProtector(new Point3D(0, 0, 0), 30),
            new SingleRodLightningProtector(new Point3D(30, 40, 0), 30));

        var result = DoubleRodProtectionService.Calculate(protector, LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(50.0, result.Result.L, precision: 5);
    }

    // -------------------------------------------------------------------------
    // Step 3: Combined zone determination
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_LgreaterThanLmax_IsCombinedFalse()
    {
        // h=5, P0_900: Lmax=5.75*5=28.75, use distance=50 > Lmax
        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(5, 50), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.False(result.Result.IsCombined);
    }

    [Fact]
    public void Calculate_LessThanLc_HcEqualsH0()
    {
        // h=30, P0_900: Lmax=5.75*30=172.5, Lc=2.5*30=75, distance=10 < Lc => hc = h0
        double h = 30;
        double expectedH0 = 0.85 * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.True(result.Result.IsCombined);
        Assert.Equal(expectedH0, result.Result.Hc, precision: 5);
    }

    [Fact]
    public void Calculate_BetweenLcAndLmax_HcCalculated()
    {
        // h=30, P0_900: Lmax=172.5, Lc=75, distance=100
        // hc = ((172.5 - 100) / (172.5 - 75)) * 25.5
        double h = 30;
        double h0 = 0.85 * h;
        double lmax = 5.75 * h;
        double lc = 2.5 * h;
        double L = 100;
        double expectedHc = ((lmax - L) / (lmax - lc)) * h0;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, L), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.True(result.Result.IsCombined);
        Assert.Equal(expectedHc, result.Result.Hc, precision: 5);
    }

    // -------------------------------------------------------------------------
    // Step 4: Protection at height hx
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_CombinedNoSag_HxBelowHc_FullRadius()
    {
        // h=30, L=10 (<=Lc=75), P0_900, hx=5
        // No sag: hc = h0 = 25.5, rx = r0*(h0-hx)/h0 = 36*(25.5-5)/25.5
        double h = 30, hx = 5;
        double h0 = 0.85 * h;
        double r0 = 1.2 * h;
        double expectedRx = r0 * (h0 - hx) / h0;
        double expectedLx = 10 / 2.0;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_900, hx);

        Assert.True(result.IsSuccess);
        Assert.True(result.Result.IsCombined);
        Assert.Equal(expectedRx, result.Result.Rx, precision: 5);
        Assert.Equal(expectedLx, result.Result.Lx, precision: 5);
    }

    [Fact]
    public void Calculate_CombinedWithSag_HxAboveHc()
    {
        // h=30, L=100 (Lc=75 < L < Lmax=172.5), P0_900, hx=20 (>hc)
        double h = 30;
        double h0 = 0.85 * h;
        double r0 = 1.2 * h;
        double lmax = 5.75 * h;
        double lc = 2.5 * h;
        double L = 100;
        double hc = ((lmax - L) / (lmax - lc)) * h0;
        double hx = 20;

        // hx >= hc? hc will be less than h0 but let's verify
        double expectedRx = r0 * (h0 - hx) / h0;
        double expectedLx = L * (h0 - hx) / (2.0 * (h0 - hc));

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, L), LightningProtectionReliability.P0_900, hx);

        Assert.True(result.IsSuccess);
        Assert.True(result.Result.IsCombined);
        Assert.Equal(expectedRx, result.Result.Rx, precision: 5);
        Assert.Equal(expectedLx, result.Result.Lx, precision: 5);
    }

    [Fact]
    public void Calculate_CombinedWithSag_HxBelowHc()
    {
        // h=30, L=100 (Lc=75 < L < Lmax=172.5), P0_900, hx=5 (<hc)
        double h = 30;
        double h0 = 0.85 * h;
        double r0 = 1.2 * h;
        double lmax = 5.75 * h;
        double lc = 2.5 * h;
        double L = 100;
        double hc = ((lmax - L) / (lmax - lc)) * h0;
        double hx = 5;

        // hx < hc: lx = L/2, rc = r0*(h0-hc)/h0, rx = rc*(hc-hx)/hc
        double expectedLx = L / 2.0;
        double rc = r0 * (h0 - hc) / h0;
        double expectedRx = rc * (hc - hx) / hc;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, L), LightningProtectionReliability.P0_900, hx);

        Assert.True(result.IsSuccess);
        Assert.True(result.Result.IsCombined);
        Assert.Equal(expectedRx, result.Result.Rx, precision: 5);
        Assert.Equal(expectedLx, result.Result.Lx, precision: 5);
    }

    // -------------------------------------------------------------------------
    // Lmax coefficients
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_Lmax_P0900_H25()
    {
        // h=25 (<=30): Lmax = 5.75 * 25
        double h = 25;
        double expected = 5.75 * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lmax, precision: 5);
    }

    [Fact]
    public void Calculate_Lmax_P0900_H60()
    {
        // h=60 (30 < h <= 100): Lmax = (5.75 - 3.57e-3*(60-30))*60
        double h = 60;
        double expected = (5.75 - 3.57e-3 * (h - 30)) * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lmax, precision: 5);
    }

    [Fact]
    public void Calculate_Lmax_P0900_H120()
    {
        // h=120 (100 < h <= 150): Lmax = 5.5 * 120
        double h = 120;
        double expected = 5.5 * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lmax, precision: 5);
    }

    [Fact]
    public void Calculate_Lmax_P0990_H25()
    {
        // h=25 (<=30): Lmax = 4.75 * 25
        double h = 25;
        double expected = 4.75 * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_990, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lmax, precision: 5);
    }

    [Fact]
    public void Calculate_Lmax_P0990_H60()
    {
        // h=60 (30 < h <= 100): Lmax = (4.75 - 3.57e-3*(60-30))*60
        double h = 60;
        double expected = (4.75 - 3.57e-3 * (h - 30)) * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_990, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lmax, precision: 5);
    }

    [Fact]
    public void Calculate_Lmax_P0990_H120()
    {
        // h=120 (100 < h <= 150): Lmax = 4.5 * 120
        double h = 120;
        double expected = 4.5 * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_990, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lmax, precision: 5);
    }

    [Fact]
    public void Calculate_Lmax_P0999_H25()
    {
        // h=25 (<=30): Lmax = 4.25 * 25
        double h = 25;
        double expected = 4.25 * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_999, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lmax, precision: 5);
    }

    [Fact]
    public void Calculate_Lmax_P0999_H60()
    {
        // h=60 (30 < h <= 100): Lmax = (4.25 - 3.57e-3*(60-30))*60
        double h = 60;
        double expected = (4.25 - 3.57e-3 * (h - 30)) * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_999, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lmax, precision: 5);
    }

    [Fact]
    public void Calculate_Lmax_P0999_H120()
    {
        // h=120 (100 < h <= 150): Lmax = 4.0 * 120
        double h = 120;
        double expected = 4.0 * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_999, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lmax, precision: 5);
    }

    // -------------------------------------------------------------------------
    // Lc coefficients
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_Lc_P0900_H25()
    {
        // h=25 (all ranges): Lc = 2.5 * 25
        double h = 25;
        double expected = 2.5 * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lc, precision: 5);
    }

    [Fact]
    public void Calculate_Lc_P0900_H60()
    {
        // h=60 (all ranges): Lc = 2.5 * 60
        double h = 60;
        double expected = 2.5 * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lc, precision: 5);
    }

    [Fact]
    public void Calculate_Lc_P0900_H120()
    {
        // h=120 (all ranges): Lc = 2.5 * 120
        double h = 120;
        double expected = 2.5 * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lc, precision: 5);
    }

    [Fact]
    public void Calculate_Lc_P0990_H25()
    {
        // h=25 (<=30): Lc = 2.25 * 25
        double h = 25;
        double expected = 2.25 * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_990, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lc, precision: 5);
    }

    [Fact]
    public void Calculate_Lc_P0990_H60()
    {
        // h=60 (30 < h <= 100): Lc = (2.25 - 1.007e-2*(60-30))*60
        double h = 60;
        double expected = (2.25 - 1.007e-2 * (h - 30)) * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_990, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lc, precision: 5);
    }

    [Fact]
    public void Calculate_Lc_P0990_H120()
    {
        // h=120 (100 < h <= 150): Lc = 1.5 * 120
        double h = 120;
        double expected = 1.5 * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_990, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lc, precision: 5);
    }

    [Fact]
    public void Calculate_Lc_P0999_H25()
    {
        // h=25 (<=30): Lc = 2.25 * 25
        double h = 25;
        double expected = 2.25 * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_999, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lc, precision: 5);
    }

    [Fact]
    public void Calculate_Lc_P0999_H60()
    {
        // h=60 (30 < h <= 100): Lc = (2.25 - 1.007e-2*(60-30))*60
        double h = 60;
        double expected = (2.25 - 1.007e-2 * (h - 30)) * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_999, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lc, precision: 5);
    }

    [Fact]
    public void Calculate_Lc_P0999_H120()
    {
        // h=120 (100 < h <= 150): Lc = 1.5 * 120
        double h = 120;
        double expected = 1.5 * h;

        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(h, 10), LightningProtectionReliability.P0_999, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Lc, precision: 5);
    }

    // -------------------------------------------------------------------------
    // Result structure
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_SuccessResult_ReturnsNonNullArea()
    {
        var result = DoubleRodProtectionService.Calculate(
            MakeSymmetricProtector(30), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Result);
    }

    [Fact]
    public void Calculate_SuccessResult_IndividualRadiiMatchSingleRod()
    {
        // h1=30, h2=60, P0_900, hx=5
        // Single rod: rod1 rx=1.2*30*(25.5-5)/25.5, rod2 rx=1.2*60*(51-5)/51
        double h1 = 30, h2 = 60, hx = 5;
        double h01 = 0.85 * h1, h02 = 0.85 * h2;
        double expectedR1 = 1.2 * h1 * (h01 - hx) / h01;
        double expectedR2 = 1.2 * h2 * (h02 - hx) / h02;

        // Use large distance so rods are not combined
        var result = DoubleRodProtectionService.Calculate(
            MakeProtector(h1, h2, 200), LightningProtectionReliability.P0_900, hx);

        Assert.True(result.IsSuccess);
        Assert.False(result.Result.IsCombined);
        Assert.Equal(expectedR1, result.Result.Rod1Area.Radius, precision: 5);
        Assert.Equal(expectedR2, result.Result.Rod2Area.Radius, precision: 5);
    }

    [Fact]
    public void Calculate_RadiusDecreasesAsHxIncreases()
    {
        var protector = MakeSymmetricProtector(50, 30);

        var resultLow = DoubleRodProtectionService.Calculate(protector, LightningProtectionReliability.P0_900, 5);
        var resultHigh = DoubleRodProtectionService.Calculate(protector, LightningProtectionReliability.P0_900, 20);

        Assert.True(resultLow.IsSuccess && resultHigh.IsSuccess);
        Assert.True(resultLow.Result.Rx > resultHigh.Result.Rx);
    }
}