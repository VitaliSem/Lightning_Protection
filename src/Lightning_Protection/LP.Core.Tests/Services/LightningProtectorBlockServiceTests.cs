using LP.Core.Models.LightningProtectors.Block;
using LP.Core.Services;

namespace LP.Core.Tests.Services;

public class LightningProtectorBlockServiceTests
{
    private static LightningProtectorBlockCreateDto MakeDto(IEnumerable<string> existingNumbers, string name,
        string type, double heightM, double? baseHeightM = null, string? description = null, string? title = null) =>
        new LightningProtectorBlockCreateDto
        {
            ExistingNumbers = existingNumbers,
            Name = name,
            Title = title!,
            Type = type,
            HeightM = heightM,
            BaseHeightM = baseHeightM,
            Description = description!,
        };

    // -------------------------------------------------------------------------
    // GetNextNumber
    // -------------------------------------------------------------------------

    [Fact]
    public void GetNextNumber_Empty_ReturnsOne()
    {
        Assert.Equal(1, LightningProtectorBlockService.GetNextNumber(Array.Empty<string>()));
    }

    [Fact]
    public void GetNextNumber_Null_ReturnsOne()
    {
        Assert.Equal(1, LightningProtectorBlockService.GetNextNumber(null!));
    }

    [Fact]
    public void GetNextNumber_Unordered_ReturnsMaxPlusOne()
    {
        Assert.Equal(6, LightningProtectorBlockService.GetNextNumber(new[] { "1", "5", "3" }));
    }

    [Fact]
    public void GetNextNumber_InvalidValues_AreIgnored()
    {
        var numbers = new[] { "2", "abc", "", "   ", null!, "-2", "2.5", "0" };

        Assert.Equal(3, LightningProtectorBlockService.GetNextNumber(numbers));
    }

    [Fact]
    public void GetNextNumber_OnlyInvalidValues_ReturnsOne()
    {
        Assert.Equal(1, LightningProtectorBlockService.GetNextNumber(new[] { "x", "-5", "1.5" }));
    }

    [Fact]
    public void GetNextNumber_ValueWithSpaces_IsParsed()
    {
        Assert.Equal(8, LightningProtectorBlockService.GetNextNumber(new[] { " 7 " }));
    }

    // -------------------------------------------------------------------------
    // Create
    // -------------------------------------------------------------------------

    [Fact]
    public void Create_NullDto_ReturnsFail()
    {
        var result = LightningProtectorBlockService.Create(null!);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(150.01)]
    public void Create_InvalidHeight_ReturnsFail(double height)
    {
        var result = LightningProtectorBlockService.Create(MakeDto(Array.Empty<string>(), "A", "B", height));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void Create_MaxHeight_ReturnsOk()
    {
        var result = LightningProtectorBlockService.Create(MakeDto(Array.Empty<string>(), "A", "B", 150));

        Assert.True(result.IsSuccess);
        Assert.Equal(150, result.Result.HeightM);
    }

    [Fact]
    public void Create_BlankNameAndType_AreEmptyStrings()
    {
        var result = LightningProtectorBlockService.Create(MakeDto(Array.Empty<string>(), null!, "   ", 25));

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Result.Name);
        Assert.Equal(string.Empty, result.Result.Type);
    }

    [Fact]
    public void Create_NameAndType_AreTrimmed()
    {
        var result = LightningProtectorBlockService.Create(MakeDto(Array.Empty<string>(), "  Rod A ", " mast ", 25));

        Assert.Equal("Rod A", result.Result.Name);
        Assert.Equal("mast", result.Result.Type);
    }

    [Fact]
    public void Create_IsActiveByDefault()
    {
        var result = LightningProtectorBlockService.Create(MakeDto(Array.Empty<string>(), "", "", 25));

        Assert.True(result.Result.IsActive);
    }

    [Fact]
    public void Create_AssignsNextNumber()
    {
        var result = LightningProtectorBlockService.Create(MakeDto(new[] { "1", "4" }, "", "", 25));

        Assert.Equal(5, result.Result.Number);
    }

    [Fact]
    public void Create_OptionalBaseHeightAndDescription_DefaultToEmpty()
    {
        var result = LightningProtectorBlockService.Create(MakeDto(Array.Empty<string>(), "", "", 25));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Result.BaseHeightM);
        Assert.Equal(string.Empty, result.Result.Description);
    }

    [Fact]
    public void Create_BaseHeightAndDescription_AreStored()
    {
        var result = LightningProtectorBlockService.Create(MakeDto(Array.Empty<string>(), "", "", 25, -1.5, "  roof mast "));

        Assert.True(result.IsSuccess);
        Assert.Equal(-1.5, result.Result.BaseHeightM);
        Assert.Equal("roof mast", result.Result.Description);
    }

    [Fact]
    public void Create_TextsAtMaxLength_ReturnsOk()
    {
        var result = LightningProtectorBlockService.Create(MakeDto(Array.Empty<string>(),
            new string('n', 100), new string('t', 100), 25, null, new string('d', 500)));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_NameTooLong_ReturnsFail()
    {
        var result = LightningProtectorBlockService.Create(MakeDto(Array.Empty<string>(), new string('n', 101), "", 25));

        Assert.False(result.IsSuccess);
        Assert.Contains("Name", result.Message);
    }

    [Fact]
    public void Create_TypeTooLong_ReturnsFail()
    {
        var result = LightningProtectorBlockService.Create(MakeDto(Array.Empty<string>(), "", new string('t', 101), 25));

        Assert.False(result.IsSuccess);
        Assert.Contains("Type", result.Message);
    }

    [Fact]
    public void Create_TitleTooLong_ReturnsFail()
    {
        var result = LightningProtectorBlockService.Create(
            MakeDto(Array.Empty<string>(), "", "", 25, title: new string('t', 101)));

        Assert.False(result.IsSuccess);
        Assert.Contains("Title", result.Message);
    }

    [Fact]
    public void Create_TitleAtMaxLength_IsTrimmedAndStored()
    {
        string title = new string('t', 100);

        var result = LightningProtectorBlockService.Create(
            MakeDto(Array.Empty<string>(), "", "", 25, title: "  " + title + " "));

        Assert.True(result.IsSuccess);
        Assert.Equal(title, result.Result.Title);
    }

    [Fact]
    public void Create_MissingTitle_IsEmpty()
    {
        var result = LightningProtectorBlockService.Create(MakeDto(Array.Empty<string>(), "", "", 25));

        Assert.Equal(string.Empty, result.Result.Title);
    }

    [Fact]
    public void Create_DescriptionTooLong_ReturnsFail()
    {
        var result = LightningProtectorBlockService.Create(MakeDto(Array.Empty<string>(), "", "", 25, null, new string('d', 501)));

        Assert.False(result.IsSuccess);
        Assert.Contains("Description", result.Message);
    }

    [Fact]
    public void ValidateTextLength_SurroundingSpaces_AreNotCounted()
    {
        Assert.Null(LightningProtectorBlockService.ValidateTextLength("  " + new string('n', 100) + "  ", 100, "Name"));
    }

    [Fact]
    public void ValidateTextLength_Null_IsValid()
    {
        Assert.Null(LightningProtectorBlockService.ValidateTextLength(null!, 100, "Name"));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Create_NonFiniteBaseHeight_ReturnsFail(double baseHeight)
    {
        var result = LightningProtectorBlockService.Create(MakeDto(Array.Empty<string>(), "", "", 25, baseHeight));

        Assert.False(result.IsSuccess);
    }

    // -------------------------------------------------------------------------
    // ToAttributeValues
    // -------------------------------------------------------------------------

    [Fact]
    public void ToAttributeValues_ContainsAllAttributes()
    {
        var data = new LightningProtectorBlockData(3, "Rod A", "mast", 25.5, true, 120.257, "roof", "Main rod");

        var values = LightningProtectorBlockService.ToAttributeValues(data);

        Assert.Equal(8, values.Count);
        Assert.Equal("3", values[LightningProtectorBlockService.AttrNumber]);
        Assert.Equal("Rod A", values[LightningProtectorBlockService.AttrName]);
        Assert.Equal("Main rod", values[LightningProtectorBlockService.AttrTitle]);
        Assert.Equal("mast", values[LightningProtectorBlockService.AttrType]);
        Assert.Equal("h=25.5", values[LightningProtectorBlockService.AttrHeightM]);
        Assert.Equal("h0=120.26", values[LightningProtectorBlockService.AttrBaseHeightM]);
        Assert.Equal("roof", values[LightningProtectorBlockService.AttrDescription]);
        Assert.Equal("true", values[LightningProtectorBlockService.AttrIsActive]);
    }

    [Fact]
    public void ToAttributeValues_MissingOptionalValues_AreEmpty()
    {
        var data = new LightningProtectorBlockData(1, "", "", 10, true);

        var values = LightningProtectorBlockService.ToAttributeValues(data);

        Assert.Equal(string.Empty, values[LightningProtectorBlockService.AttrBaseHeightM]);
        Assert.Equal(string.Empty, values[LightningProtectorBlockService.AttrDescription]);
        Assert.Equal(string.Empty, values[LightningProtectorBlockService.AttrTitle]);
    }

    [Fact]
    public void ToAttributeValues_ZeroBaseHeight_HasPrefix()
    {
        var data = new LightningProtectorBlockData(1, "", "", 10, true, 0);

        var values = LightningProtectorBlockService.ToAttributeValues(data);

        Assert.Equal("h0=0", values[LightningProtectorBlockService.AttrBaseHeightM]);
    }

    [Fact]
    public void ToAttributeValues_HeightIsInvariantAndRounded()
    {
        var data = new LightningProtectorBlockData(1, "", "", 30.456, false);

        var values = LightningProtectorBlockService.ToAttributeValues(data);

        Assert.Equal("h=30.46", values[LightningProtectorBlockService.AttrHeightM]);
        Assert.Equal("false", values[LightningProtectorBlockService.AttrIsActive]);
    }

    [Fact]
    public void ToAttributeValues_KeysAreCaseInsensitive()
    {
        var data = new LightningProtectorBlockData(1, "", "", 10, true);

        var values = LightningProtectorBlockService.ToAttributeValues(data);

        Assert.Equal("1", values["number"]);
    }
}
