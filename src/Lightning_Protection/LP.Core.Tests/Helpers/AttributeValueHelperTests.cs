using LP.Core.Helpers;

namespace LP.Core.Tests.Helpers;

public class AttributeValueHelperTests
{
    [Theory]
    [InlineData("  Rod A ", "Rod A")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Clean_TrimsAndReplacesNull(string? value, string expected)
    {
        Assert.Equal(expected, AttributeValueHelper.Clean(value!));
    }

    [Fact]
    public void GetValue_ExistingTag_ReturnsValue()
    {
        var values = new Dictionary<string, string> { { "NAME", "Rod A" } };

        Assert.Equal("Rod A", AttributeValueHelper.GetValue(values, "NAME"));
    }

    [Fact]
    public void GetValue_RespectsDictionaryComparer()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "NAME", "Rod A" } };

        Assert.Equal("Rod A", AttributeValueHelper.GetValue(values, "name"));
    }

    [Fact]
    public void GetValue_MissingTagOrNullValue_ReturnsEmpty()
    {
        var values = new Dictionary<string, string> { { "NAME", null! } };

        Assert.Equal(string.Empty, AttributeValueHelper.GetValue(values, "NAME"));
        Assert.Equal(string.Empty, AttributeValueHelper.GetValue(values, "TITLE"));
    }

    [Fact]
    public void GetValue_NullDictionary_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, AttributeValueHelper.GetValue(null!, "NAME"));
    }

    [Theory]
    [InlineData("3", 3)]
    [InlineData(" 12 ", 12)]
    public void TryParseNumber_PositiveInteger_ReturnsTrue(string value, int expected)
    {
        Assert.True(AttributeValueHelper.TryParseNumber(value, out int number));
        Assert.Equal(expected, number);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("abc")]
    public void TryParseNumber_Invalid_ReturnsFalse(string? value)
    {
        Assert.False(AttributeValueHelper.TryParseNumber(value!, out _));
    }
}
