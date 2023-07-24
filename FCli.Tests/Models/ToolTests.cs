using FCli.Common.Exceptions;
using FCli.Models;
using static FCli.Models.Args;

namespace FCli.Tests.Models;

public class ToolTests : Tool
{
    private readonly string _name = "Test";
    [Fact]
    public void Tool_FlagHasValue_Value()
    {
        var flag = new Flag("flag", "value");

        var act = () => FlagHasValue(flag, _name);

        act.Should().NotThrow();
    }
    
    [Fact]
    public void Tool_FlagHasValue_NoValue()
    {
        var flag = new Flag("flag", "");

        var act = () => FlagHasValue(flag, _name);

        act.Should().ThrowExactly<FlagException>();
    }

    [Fact]
    public void Tool_FlagHasNoValue_Value()
    {
        var flag = new Flag("flag", "value");

        var act = () => FlagHasNoValue(flag, _name);

        act.Should().ThrowExactly<FlagException>();
    }

    [Fact]
    public void Tool_FlagHasNoValue_NoValue()
    {
        var flag = new Flag("flag", "");

        var act = () => FlagHasNoValue(flag, _name);

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
        var act = () => ValidateUrl(url, _name);
        var uri = ValidateUrl(url, _name);

        act.Should().NotThrow();
        uri.Should().BeAssignableTo(typeof(Uri));
    }

    [Theory]
    [InlineData("http:/google.com")]
    [InlineData("https//googlecom")]
    [InlineData("agaefafa")]
    public void Tool_ValidateUrl_Invalid(string url)
    {
        var act = () => ValidateUrl(url, _name);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Theory]
    [InlineData("afasfafaf")]
    [InlineData("C:/nowhere")]
    public void Tool_ValidatePath_Invalid(string url)
    {
        var act = () => ValidatePath(url, _name);

        act.Should().ThrowExactly<ArgumentException>();
    }
}