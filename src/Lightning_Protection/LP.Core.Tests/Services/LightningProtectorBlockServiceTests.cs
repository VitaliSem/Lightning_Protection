using LP.Core.Constants;
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
        Assert.Equal("3", values[LightningProtectorAttributes.Number]);
        Assert.Equal("Rod A", values[LightningProtectorAttributes.Name]);
        Assert.Equal("Main rod", values[LightningProtectorAttributes.Title]);
        Assert.Equal("mast", values[LightningProtectorAttributes.Type]);
        Assert.Equal("h=25.5", values[LightningProtectorAttributes.HeightM]);
        Assert.Equal("h0=120.26", values[LightningProtectorAttributes.BaseHeightM]);
        Assert.Equal("roof", values[LightningProtectorAttributes.Description]);
        Assert.Equal("true", values[LightningProtectorAttributes.IsActive]);
    }

    [Fact]
    public void ToAttributeValues_MissingOptionalValues_AreEmpty()
    {
        var data = new LightningProtectorBlockData(1, "", "", 10, true);

        var values = LightningProtectorBlockService.ToAttributeValues(data);

        Assert.Equal(string.Empty, values[LightningProtectorAttributes.BaseHeightM]);
        Assert.Equal(string.Empty, values[LightningProtectorAttributes.Description]);
        Assert.Equal(string.Empty, values[LightningProtectorAttributes.Title]);
    }

    [Fact]
    public void ToAttributeValues_ZeroBaseHeight_HasPrefix()
    {
        var data = new LightningProtectorBlockData(1, "", "", 10, true, 0);

        var values = LightningProtectorBlockService.ToAttributeValues(data);

        Assert.Equal("h0=0", values[LightningProtectorAttributes.BaseHeightM]);
    }

    [Fact]
    public void ToAttributeValues_HeightIsInvariantAndRounded()
    {
        var data = new LightningProtectorBlockData(1, "", "", 30.456, false);

        var values = LightningProtectorBlockService.ToAttributeValues(data);

        Assert.Equal("h=30.46", values[LightningProtectorAttributes.HeightM]);
        Assert.Equal("false", values[LightningProtectorAttributes.IsActive]);
    }

    [Fact]
    public void ToAttributeValues_KeysAreCaseInsensitive()
    {
        var data = new LightningProtectorBlockData(1, "", "", 10, true);

        var values = LightningProtectorBlockService.ToAttributeValues(data);

        Assert.Equal("1", values["number"]);
    }

    // -------------------------------------------------------------------------
    // FromAttributeValues
    // -------------------------------------------------------------------------

    [Fact]
    public void FromAttributeValues_RoundTripsToAttributeValues()
    {
        var data = new LightningProtectorBlockData(3, "Rod A", "mast", 25.5, false, -1.25, "roof", "Main rod");

        var result = LightningProtectorBlockService.FromAttributeValues(LightningProtectorBlockService.ToAttributeValues(data));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Result.Number);
        Assert.Equal("Rod A", result.Result.Name);
        Assert.Equal("Main rod", result.Result.Title);
        Assert.Equal("mast", result.Result.Type);
        Assert.Equal(25.5, result.Result.HeightM);
        Assert.Equal(-1.25, result.Result.BaseHeightM);
        Assert.Equal("roof", result.Result.Description);
        Assert.False(result.Result.IsActive);
    }

    [Fact]
    public void FromAttributeValues_EmptyBaseHeight_IsNull()
    {
        var values = LightningProtectorBlockService.ToAttributeValues(new LightningProtectorBlockData(1, "", "", 10, true));

        var result = LightningProtectorBlockService.FromAttributeValues(values);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Result.BaseHeightM);
        Assert.True(result.Result.IsActive);
    }

    [Fact]
    public void FromAttributeValues_MissingOptionalTags_AreDefaults()
    {
        var values = new Dictionary<string, string> { { "number", "2" }, { "height_m", "h=12" } };

        var result = LightningProtectorBlockService.FromAttributeValues(values);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Result.Name);
        Assert.Equal(12, result.Result.HeightM);
        Assert.True(result.Result.IsActive);
    }

    [Theory]
    [InlineData("", "h=10")]
    [InlineData("x", "h=10")]
    [InlineData("1", "")]
    [InlineData("1", "h=abc")]
    public void FromAttributeValues_InvalidNumberOrHeight_ReturnsFail(string number, string height)
    {
        var values = new Dictionary<string, string>
        {
            { LightningProtectorAttributes.Number, number },
            { LightningProtectorAttributes.HeightM, height },
        };

        Assert.False(LightningProtectorBlockService.FromAttributeValues(values).IsSuccess);
    }

    [Fact]
    public void FromAttributeValues_InvalidBaseHeight_ReturnsFail()
    {
        var values = new Dictionary<string, string>
        {
            { LightningProtectorAttributes.Number, "1" },
            { LightningProtectorAttributes.HeightM, "h=10" },
            { LightningProtectorAttributes.BaseHeightM, "h0=?" },
        };

        Assert.False(LightningProtectorBlockService.FromAttributeValues(values).IsSuccess);
    }

    [Fact]
    public void FromAttributeValues_Null_ReturnsFail()
    {
        Assert.False(LightningProtectorBlockService.FromAttributeValues(null!).IsSuccess);
    }

    // -------------------------------------------------------------------------
    // Update
    // -------------------------------------------------------------------------

    private static readonly LightningProtectorBlockData Current =
        new LightningProtectorBlockData(7, "Rod A", "mast", 25, true, 3, "roof", "Main rod");

    private static LightningProtectorBlockUpdateDto MakeUpdate(double heightM = 30, double? baseHeightM = null,
        string name = "", string type = "", string title = "", string description = "", bool isActive = true) =>
        new LightningProtectorBlockUpdateDto
        {
            Name = name,
            Title = title,
            Type = type,
            HeightM = heightM,
            BaseHeightM = baseHeightM,
            Description = description,
            IsActive = isActive,
        };

    [Fact]
    public void Update_KeepsNumberAndAppliesAllValues()
    {
        var result = LightningProtectorBlockService.Update(Current,
            MakeUpdate(40, -2, " Rod B ", " tower ", " Spare ", " yard ", false));

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Result.Number);
        Assert.Equal("Rod B", result.Result.Name);
        Assert.Equal("tower", result.Result.Type);
        Assert.Equal("Spare", result.Result.Title);
        Assert.Equal("yard", result.Result.Description);
        Assert.Equal(40, result.Result.HeightM);
        Assert.Equal(-2, result.Result.BaseHeightM);
        Assert.False(result.Result.IsActive);
    }

    [Fact]
    public void Update_ClearedBaseHeight_IsNull()
    {
        var result = LightningProtectorBlockService.Update(Current, MakeUpdate(baseHeightM: null));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Result.BaseHeightM);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(150.01)]
    public void Update_InvalidHeight_ReturnsFail(double height)
    {
        Assert.False(LightningProtectorBlockService.Update(Current, MakeUpdate(height)).IsSuccess);
    }

    [Fact]
    public void Update_MaxHeight_ReturnsOk()
    {
        Assert.True(LightningProtectorBlockService.Update(Current, MakeUpdate(150)).IsSuccess);
    }

    [Fact]
    public void Update_NonFiniteBaseHeight_ReturnsFail()
    {
        Assert.False(LightningProtectorBlockService.Update(Current, MakeUpdate(baseHeightM: double.NaN)).IsSuccess);
    }

    [Fact]
    public void Update_TypeTooLong_ReturnsFail()
    {
        var result = LightningProtectorBlockService.Update(Current, MakeUpdate(type: new string('t', 101)));

        Assert.False(result.IsSuccess);
        Assert.Contains("Type", result.Message);
    }

    [Fact]
    public void Update_DescriptionTooLong_ReturnsFail()
    {
        var result = LightningProtectorBlockService.Update(Current, MakeUpdate(description: new string('d', 501)));

        Assert.False(result.IsSuccess);
        Assert.Contains("Description", result.Message);
    }

    [Fact]
    public void Update_NullArguments_ReturnFail()
    {
        Assert.False(LightningProtectorBlockService.Update(null!, MakeUpdate()).IsSuccess);
        Assert.False(LightningProtectorBlockService.Update(Current, null!).IsSuccess);
    }

    [Fact]
    public void EditableTags_ExcludeNumber()
    {
        Assert.Equal(7, LightningProtectorAttributes.EditableTags.Count);
        Assert.DoesNotContain(LightningProtectorAttributes.Number, LightningProtectorAttributes.EditableTags);
        Assert.Contains(LightningProtectorAttributes.Type, LightningProtectorAttributes.EditableTags);
    }
}
