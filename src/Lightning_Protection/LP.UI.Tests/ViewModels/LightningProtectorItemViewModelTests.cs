using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.Block;
using LP.UI.ViewModels;

namespace LP.UI.Tests.ViewModels;

public class LightningProtectorItemViewModelTests
{
    private static LightningProtectorItemViewModel Create(LightningProtectorBlockData data, Point3D? position = null,
        string? layoutName = null) =>
        new(new LightningProtectorListEntry(data, position ?? new Point3D(0, 0), layoutName));

    [Fact]
    public void AllValues_AreShown()
    {
        var item = Create(new LightningProtectorBlockData(3, "MP-3", "Rod", 25.5, true, -1.25, "On the roof", "Main rod"));

        Assert.Equal("#3", item.NumberText);
        Assert.Equal("MP-3", item.Name);
        Assert.Equal("Main rod", item.Title);
        Assert.Equal("Rod", item.Type);
        Assert.Equal("25.5", item.HeightText);
        Assert.Equal("-1.25", item.BaseHeightText);
        Assert.Equal("On the roof", item.Description);
        Assert.Equal("Active", item.StatusText);
    }

    [Fact]
    public void MissingOptionalValues_AreEmpty()
    {
        var item = Create(new LightningProtectorBlockData(4, null, null, 10, false));

        Assert.Equal("", item.Name);
        Assert.Equal("", item.Title);
        Assert.Equal("", item.Type);
        Assert.Equal("", item.BaseHeightText);
        Assert.Null(item.BaseHeightM);
        Assert.Equal("", item.Description);
        Assert.Equal("Inactive", item.StatusText);
        Assert.Equal("#4", item.DisplayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HasDescription_BlankDescription_IsFalse(string? description)
    {
        var item = Create(new LightningProtectorBlockData(1, "", "", 10, true, description: description));

        Assert.False(item.HasDescription);
    }

    [Fact]
    public void HasDescription_WithDescription_IsTrue()
    {
        var item = Create(new LightningProtectorBlockData(1, "", "", 10, true, description: "Roof"));

        Assert.True(item.HasDescription);
    }

    [Theory]
    [InlineData(1012.75, 2491.6, "1012.75; 2491.6")]
    [InlineData(-5, 0, "-5; 0")]
    [InlineData(1.004, 2.3333, "1; 2.33")]
    public void PositionText_FormatsXAndY(double x, double y, string expected)
    {
        var item = Create(new LightningProtectorBlockData(1, "", "", 10, true), new Point3D(x, y));

        Assert.Equal(expected, item.PositionText);
        Assert.Equal(x, item.X);
        Assert.Equal(y, item.Y);
    }

    [Fact]
    public void ModelSpace_IsNotPaperSpace()
    {
        var item = Create(new LightningProtectorBlockData(1, "", "", 10, true));

        Assert.False(item.IsInPaperSpace);
        Assert.Equal("", item.PositionToolTip);
    }

    [Fact]
    public void PaperSpace_ToolTipNamesLayout()
    {
        var item = Create(new LightningProtectorBlockData(1, "", "", 10, true), layoutName: "Layout1");

        Assert.True(item.IsInPaperSpace);
        Assert.Equal("Paper-space coordinates (layout \"Layout1\").", item.PositionToolTip);
    }

    [Fact]
    public void FocusToolTip_NamesProtector()
    {
        var item = Create(new LightningProtectorBlockData(7, "", "", 10, true));

        Assert.Equal("Show #7 on the drawing", item.FocusToolTip);
    }
}
