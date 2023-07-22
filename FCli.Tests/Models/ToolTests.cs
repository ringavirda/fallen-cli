using FCli.Common.Exceptions;
using FCli.Models;

namespace FCli.Tests.Models;

public class ToolTests : Tool
{
    [Fact]
    public void Tool_FlagHasValue_Value()
    {
        var flag = new Flag("flag", "value");

        var act = () => FlagHasValue(flag);

        act.Should().NotThrow();
    }
    
    [Fact]
    public void Tool_FlagHasValue_NoValue()
    {
        var flag = new Flag("flag", "");

        var act = () => FlagHasValue(flag);

        act.Should().ThrowExactly<FlagException>();
    }

    [Fact]
    public void Tool_FlagHasNoValue_Value()
    {
        var flag = new Flag("flag", "value");

        var act = () => FlagHasNoValue(flag);

        act.Should().ThrowExactly<FlagException>();
    }

    [Fact]
    public void Tool_FlagHasNoValue_NoValue()
    {
        var flag = new Flag("flag", "");

        var act = () => FlagHasNoValue(flag);

        act.Should().NotThrow();
    }

    [Fact]
    public void Tool_UnknownFlag_Throws()
    {
        var flag = new Flag("flag", "");

        var act = () => UnknownFlag(flag, "tool");

        act.Should().ThrowExactly<FlagException>();
    }

    [Theory]
    [InlineData("http://google.com")]
    [InlineData("https://google.com")]
    [InlineData("http://www.google.com")]
    [InlineData("https://www.google.ua.com/page")]
    public void Tool_ValidateUrl_Valid(string url)
    {
        var act = () => ValidateUrl(url);
        var uri = ValidateUrl(url);

        act.Should().NotThrow();
        uri.Should().BeAssignableTo(typeof(Uri));
    }

    [Theory]
    [InlineData("http:/google.com")]
    [InlineData("https//googlecom")]
    [InlineData("agaefafa")]
    public void Tool_ValidateUrl_Invalid(string url)
    {
        var act = () => ValidateUrl(url);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Theory]
    [InlineData("afasfafaf")]
    [InlineData("C:/nowhere")]
    public void Tool_ValidatePath_Invalid(string url)
    {
        var act = () => ValidatePath(url);

        act.Should().ThrowExactly<ArgumentException>();
    }
}