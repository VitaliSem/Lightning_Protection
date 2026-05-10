using LP.Core.Enums;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors;
using LP.Core.Services.SN_4_04_03;

namespace LP.Core.Tests.Services.SN_4_04_03;

public class SingleWireProtectionServiceTests
{
    private static SingleWireLightningProtector MakeProtector(double height1, double height2) =>
        new SingleWireLightningProtector(new Point3D(0, 0, 0), height1, new Point3D(50, 0, 0), height2);

    private static SingleWireLightningProtector MakeSymmetricProtector(double height) =>
        MakeProtector(height, height);

    // -------------------------------------------------------------------------
    // Input validation
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_Height1Zero_ReturnsFail()
    {
        var result = SingleWireProtectionService.Calculate(MakeProtector(0, 30), LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void Calculate_Height2Zero_ReturnsFail()
    {
        var result = SingleWireProtectionService.Calculate(MakeProtector(30, 0), LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_Height1Negative_ReturnsFail()
    {
        var result = SingleWireProtectionService.Calculate(MakeProtector(-5, 30), LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_Height1Exceeds150_ReturnsFail()
    {
        var result = SingleWireProtectionService.Calculate(MakeProtector(151, 30), LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_Height2Exceeds150_ReturnsFail()
    {
        var result = SingleWireProtectionService.Calculate(MakeProtector(30, 151), LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_HxNegative_ReturnsFail()
    {
        var result = SingleWireProtectionService.Calculate(MakeSymmetricProtector(30), LightningProtectionReliability.P0_900, -1);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_HxEqualToHeight1_ReturnsFail()
    {
        var result = SingleWireProtectionService.Calculate(MakeProtector(20, 30), LightningProtectionReliability.P0_900, 20);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_HxEqualToHeight2_ReturnsFail()
    {
        var result = SingleWireProtectionService.Calculate(MakeProtector(30, 20), LightningProtectionReliability.P0_900, 20);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_HxGreaterThanBothHeights_ReturnsFail()
    {
        var result = SingleWireProtectionService.Calculate(MakeSymmetricProtector(30), LightningProtectionReliability.P0_900, 35);

        Assert.False(result.IsSuccess);
    }

    // -------------------------------------------------------------------------
    // Reliability 0.900 — h0=0.87h, r0=1.5h (all ranges)
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_P0900_H50_HxGround_ReturnsBothRadiiEqual()
    {
        // h=50, h0=0.87*50=43.5, r0=1.5*50=75, hx=0 => rx=75
        var result = SingleWireProtectionService.Calculate(MakeSymmetricProtector(50), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(75.0, result.Result.Radius1, precision: 5);
        Assert.Equal(75.0, result.Result.Radius2, precision: 5);
    }

    [Fact]
    public void Calculate_P0900_H50_HxHalf_ReturnsExpectedRadius()
    {
        // h=50, h0=43.5, r0=75, hx=20 => rx = 75*(43.5-20)/43.5
        double h = 50, hx = 20;
        double h0 = 0.87 * h;
        double r0 = 1.5 * h;
        double expected = r0 * (h0 - hx) / h0;

        var result = SingleWireProtectionService.Calculate(MakeSymmetricProtector(h), LightningProtectionReliability.P0_900, hx);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Radius1, precision: 5);
        Assert.Equal(expected, result.Result.Radius2, precision: 5);
    }

    [Fact]
    public void Calculate_P0900_AsymmetricHeights_ReturnsDifferentRadii()
    {
        // h1=30, h2=60, hx=0
        double h1 = 30, h2 = 60, hx = 0;
        double expectedR1 = 1.5 * h1;
        double expectedR2 = 1.5 * h2;

        var result = SingleWireProtectionService.Calculate(MakeProtector(h1, h2), LightningProtectionReliability.P0_900, hx);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedR1, result.Result.Radius1, precision: 5);
        Assert.Equal(expectedR2, result.Result.Radius2, precision: 5);
    }

    // -------------------------------------------------------------------------
    // Reliability 0.990
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_P0990_H20_HxGround_ReturnsExpectedRadius()
    {
        // h=20 (<=30): h0=0.8*20=16, r0=0.95*20=19 => rx=19
        var result = SingleWireProtectionService.Calculate(MakeSymmetricProtector(20), LightningProtectionReliability.P0_990, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(19.0, result.Result.Radius1, precision: 5);
        Assert.Equal(19.0, result.Result.Radius2, precision: 5);
    }

    [Fact]
    public void Calculate_P0990_H60_HxGround_ReturnsExpectedRadius()
    {
        // h=60 (30 < h <= 100): h0=0.8*60=48, r0=(0.95-7.14e-3*(60-30))*60
        double h = 60;
        double h0 = 0.8 * h;
        double r0 = (0.95 - 7.14e-3 * (h - 30)) * h;
        double expected = r0 * h0 / h0;

        var result = SingleWireProtectionService.Calculate(MakeSymmetricProtector(h), LightningProtectionReliability.P0_990, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Radius1, precision: 5);
        Assert.Equal(expected, result.Result.Radius2, precision: 5);
    }

    [Fact]
    public void Calculate_P0990_H120_HxGround_ReturnsExpectedRadius()
    {
        // h=120 (100 < h <= 150): h0=0.8*120=96, r0=(0.9-1e-3*(120-100))*120
        double h = 120;
        double h0 = 0.8 * h;
        double r0 = (0.9 - 1e-3 * (h - 100)) * h;
        double expected = r0 * h0 / h0;

        var result = SingleWireProtectionService.Calculate(MakeSymmetricProtector(h), LightningProtectionReliability.P0_990, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Radius1, precision: 5);
        Assert.Equal(expected, result.Result.Radius2, precision: 5);
    }

    // -------------------------------------------------------------------------
    // Reliability 0.999
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_P0999_H20_HxGround_ReturnsExpectedRadius()
    {
        // h=20 (<=30): h0=0.7*20=14, r0=0.7*20=14 => rx=14
        var result = SingleWireProtectionService.Calculate(MakeSymmetricProtector(20), LightningProtectionReliability.P0_999, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(14.0, result.Result.Radius1, precision: 5);
        Assert.Equal(14.0, result.Result.Radius2, precision: 5);
    }

    [Fact]
    public void Calculate_P0999_H60_HxGround_ReturnsExpectedRadius()
    {
        // h=60 (30 < h <= 100): h0=(0.75-4.28e-4*(60-30))*60, r0=(0.7-1.43e-3*(60-30))*60
        double h = 60;
        double h0 = (0.75 - 4.28e-4 * (h - 30)) * h;
        double r0 = (0.7 - 1.43e-3 * (h - 30)) * h;
        double expected = r0 * h0 / h0;

        var result = SingleWireProtectionService.Calculate(MakeSymmetricProtector(h), LightningProtectionReliability.P0_999, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Radius1, precision: 5);
        Assert.Equal(expected, result.Result.Radius2, precision: 5);
    }

    [Fact]
    public void Calculate_P0999_H120_HxGround_ReturnsExpectedRadius()
    {
        // h=120 (100 < h <= 150): h0=(0.72-1e-3*(120-100))*120, r0=(0.6-1e-3*(120-100))*120
        double h = 120;
        double h0 = (0.72 - 1e-3 * (h - 100)) * h;
        double r0 = (0.6 - 1e-3 * (h - 100)) * h;
        double expected = r0 * h0 / h0;

        var result = SingleWireProtectionService.Calculate(MakeSymmetricProtector(h), LightningProtectionReliability.P0_999, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Radius1, precision: 5);
        Assert.Equal(expected, result.Result.Radius2, precision: 5);
    }

    // -------------------------------------------------------------------------
    // hx >= h0 => radius = 0
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_HxExceedsConeHeight_ReturnsZeroRadius()
    {
        // h=20, P0_900: h0=0.87*20=17.4, hx=18 => rx=0
        var result = SingleWireProtectionService.Calculate(MakeSymmetricProtector(20), LightningProtectionReliability.P0_900, 18);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_HxExceedsConeHeightForOnePoint_ReturnsZeroForThatPoint()
    {
        // h1=20 (h0=17.4), h2=50 (h0=43.5), hx=18
        // => Radius1=0, Radius2>0
        double h2 = 50, hx = 18;
        double h0_2 = 0.87 * h2;
        double r0_2 = 1.5 * h2;
        double expectedR2 = r0_2 * (h0_2 - hx) / h0_2;

        var result = SingleWireProtectionService.Calculate(MakeProtector(20, h2), LightningProtectionReliability.P0_900, hx);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.0, result.Result.Radius1, precision: 5);
        Assert.Equal(expectedR2, result.Result.Radius2, precision: 5);
    }

    // -------------------------------------------------------------------------
    // Result structure
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_SuccessResult_PinPointsMatchProtector()
    {
        var pinPoint1 = new Point3D(0, 0, 0);
        var pinPoint2 = new Point3D(10, 5, 0);
        var protector = new SingleWireLightningProtector(pinPoint1, 30, pinPoint2, 30);

        var result = SingleWireProtectionService.Calculate(protector, LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(pinPoint1, result.Result.PinPoint1);
        Assert.Equal(pinPoint2, result.Result.PinPoint2);
    }

    [Fact]
    public void Calculate_SuccessResult_RadiiArePositive()
    {
        var result = SingleWireProtectionService.Calculate(MakeSymmetricProtector(50), LightningProtectionReliability.P0_990, 10);

        Assert.True(result.IsSuccess);
        Assert.True(result.Result.Radius1 > 0);
        Assert.True(result.Result.Radius2 > 0);
    }

    [Fact]
    public void Calculate_RadiiDecreaseAsHxIncreases()
    {
        var protector = MakeSymmetricProtector(50);

        var resultLow = SingleWireProtectionService.Calculate(protector, LightningProtectionReliability.P0_900, 5);
        var resultHigh = SingleWireProtectionService.Calculate(protector, LightningProtectionReliability.P0_900, 30);

        Assert.True(resultLow.IsSuccess && resultHigh.IsSuccess);
        Assert.True(resultLow.Result.Radius1 > resultHigh.Result.Radius1);
        Assert.True(resultLow.Result.Radius2 > resultHigh.Result.Radius2);
    }
}
