using LP.Core.Enums;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors;
using LP.Core.Services;

namespace LP.Core.Tests.Services;

public class SingleRodProtectionServiceTests
{
    private static SingleRodLightningProtector MakeProtector(double height) =>
        new SingleRodLightningProtector(new Point3D(0, 0, 0), height);

    // -------------------------------------------------------------------------
    // Input validation
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_HeightZero_ReturnsFail()
    {
        var result = SingleRodProtectionService.Calculate(MakeProtector(0), LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void Calculate_HeightNegative_ReturnsFail()
    {
        var result = SingleRodProtectionService.Calculate(MakeProtector(-1), LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_HeightExceeds150_ReturnsFail()
    {
        var result = SingleRodProtectionService.Calculate(MakeProtector(151), LightningProtectionReliability.P0_900, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_HxNegative_ReturnsFail()
    {
        var result = SingleRodProtectionService.Calculate(MakeProtector(30), LightningProtectionReliability.P0_900, -1);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_HxEqualToHeight_ReturnsFail()
    {
        var result = SingleRodProtectionService.Calculate(MakeProtector(30), LightningProtectionReliability.P0_900, 30);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Calculate_HxGreaterThanHeight_ReturnsFail()
    {
        var result = SingleRodProtectionService.Calculate(MakeProtector(30), LightningProtectionReliability.P0_900, 35);

        Assert.False(result.IsSuccess);
    }

    // -------------------------------------------------------------------------
    // Reliability 0.900
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_P0900_H50_HxGround_ReturnsExpectedRadius()
    {
        // h=50, h0=0.85*50=42.5, r0=1.2*50=60, hx=0 => rx=60
        var result = SingleRodProtectionService.Calculate(MakeProtector(50), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(60.0, result.Result.Radius, precision: 5);
    }

    [Fact]
    public void Calculate_P0900_H50_HxHalf_ReturnsExpectedRadius()
    {
        // h=50, h0=42.5, r0=60, hx=25 => rx = 60*(42.5-25)/42.5
        double expected = 60.0 * (42.5 - 25.0) / 42.5;
        var result = SingleRodProtectionService.Calculate(MakeProtector(50), LightningProtectionReliability.P0_900, 25);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Radius, precision: 5);
    }

    [Fact]
    public void Calculate_P0900_H120_HxGround_ReturnsExpectedRadius()
    {
        // h=120 (100 < h <= 150): h0=0.85*120=102, r0=(1.2-1e-3*(120-100))*120=141.6
        double h = 120;
        double h0 = 0.85 * h;
        double r0 = (1.2 - 1e-3 * (h - 100)) * h;
        double expected = r0 * h0 / h0; // rx at hx=0

        var result = SingleRodProtectionService.Calculate(MakeProtector(h), LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Radius, precision: 5);
    }

    // -------------------------------------------------------------------------
    // Reliability 0.990
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_P0990_H20_HxGround_ReturnsExpectedRadius()
    {
        // h=20 (<=30): h0=0.8*20=16, r0=0.8*20=16 => rx=16
        var result = SingleRodProtectionService.Calculate(MakeProtector(20), LightningProtectionReliability.P0_990, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(16.0, result.Result.Radius, precision: 5);
    }

    [Fact]
    public void Calculate_P0990_H60_HxGround_ReturnsExpectedRadius()
    {
        // h=60 (30 < h <= 100): h0=0.8*60=48, r0=(0.8-1.43e-3*(60-30))*60
        double h = 60;
        double h0 = 0.8 * h;
        double r0 = (0.8 - 1.43e-3 * (h - 30)) * h;
        double expected = r0 * h0 / h0;

        var result = SingleRodProtectionService.Calculate(MakeProtector(h), LightningProtectionReliability.P0_990, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Radius, precision: 5);
    }

    [Fact]
    public void Calculate_P0990_H120_HxGround_ReturnsExpectedRadius()
    {
        // h=120 (100 < h <= 150): h0=(0.8-1e-3*(120-100))*120=93.6, r0=0.7*120=84
        double h = 120;
        double h0 = (0.8 - 1e-3 * (h - 100)) * h;
        double r0 = 0.7 * h;
        double expected = r0 * h0 / h0;

        var result = SingleRodProtectionService.Calculate(MakeProtector(h), LightningProtectionReliability.P0_990, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Radius, precision: 5);
    }

    // -------------------------------------------------------------------------
    // Reliability 0.999
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_P0999_H20_HxGround_ReturnsExpectedRadius()
    {
        // h=20 (<=30): h0=0.7*20=14, r0=0.6*20=12 => rx=12
        var result = SingleRodProtectionService.Calculate(MakeProtector(20), LightningProtectionReliability.P0_999, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(12.0, result.Result.Radius, precision: 5);
    }

    [Fact]
    public void Calculate_P0999_H60_HxGround_ReturnsExpectedRadius()
    {
        // h=60 (30 < h <= 100): h0=(0.7-7.14e-4*(60-30))*60, r0=(0.6-1.43e-3*(60-30))*60
        double h = 60;
        double h0 = (0.7 - 7.14e-4 * (h - 30)) * h;
        double r0 = (0.6 - 1.43e-3 * (h - 30)) * h;
        double expected = r0 * h0 / h0;

        var result = SingleRodProtectionService.Calculate(MakeProtector(h), LightningProtectionReliability.P0_999, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Radius, precision: 5);
    }

    [Fact]
    public void Calculate_P0999_H120_HxGround_ReturnsExpectedRadius()
    {
        // h=120 (100 < h <= 150): h0=(0.65-1e-3*(120-100))*120, r0=(0.5-2e-3*(120-100))*120
        double h = 120;
        double h0 = (0.65 - 1e-3 * (h - 100)) * h;
        double r0 = (0.5 - 2e-3 * (h - 100)) * h;
        double expected = r0 * h0 / h0;

        var result = SingleRodProtectionService.Calculate(MakeProtector(h), LightningProtectionReliability.P0_999, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Result.Radius, precision: 5);
    }

    // -------------------------------------------------------------------------
    // Result structure
    // -------------------------------------------------------------------------

    [Fact]
    public void Calculate_SuccessResult_CenterMatchesProtectorPosition()
    {
        var position = new Point3D(5.0, 10.0, 0.0);
        var protector = new SingleRodLightningProtector(position, 30.0);

        var result = SingleRodProtectionService.Calculate(protector, LightningProtectionReliability.P0_900, 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(position, result.Result.Center);
    }

    [Fact]
    public void Calculate_SuccessResult_RadiusIsPositive()
    {
        var result = SingleRodProtectionService.Calculate(MakeProtector(30), LightningProtectionReliability.P0_990, 10);

        Assert.True(result.IsSuccess);
        Assert.True(result.Result.Radius > 0);
    }

    [Fact]
    public void Calculate_RadiusDecreasesAsHxIncreases()
    {
        var protector = MakeProtector(50);
        var resultLow = SingleRodProtectionService.Calculate(protector, LightningProtectionReliability.P0_900, 5);
        var resultHigh = SingleRodProtectionService.Calculate(protector, LightningProtectionReliability.P0_900, 40);

        Assert.True(resultLow.IsSuccess && resultHigh.IsSuccess);
        Assert.True(resultLow.Result.Radius > resultHigh.Result.Radius);
    }
}
