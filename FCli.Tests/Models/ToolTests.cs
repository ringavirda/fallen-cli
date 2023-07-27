using FCli.Exceptions;
using FCli.Models;
using FCli.Models.Types;
using static FCli.Models.Args;

namespace FCli.Tests.Models;

public class ToolTests : Tool
{
    public override string Name => "Test";
    public override string Description => "";
    public override List<string> Selectors => new();
    public override ToolType Type => ToolType.None;
    public override Action<string, List<Flag>> Action => (str, flags) => { };

    public ToolTests() 
        : base(
            TestRepository.FormatterFake.Object,
            TestRepository.ResourcesFake.Object
        ) { }

    [Fact]
    public void Tool_FlagHasValue_Value()
    {
        var flag = new Flag("flag", "value");

        var act = () => FlagHasValue(flag, Name);

        act.Should().NotThrow();
    }

    [Fact]
    public void Tool_FlagHasValue_NoValue()
    {
        var flag = new Flag("flag", "");

        var act = () => FlagHasValue(flag, Name);

        act.Should().ThrowExactly<FlagException>();
    }

    [Fact]
    public void Tool_FlagHasNoValue_Value()
    {
        var flag = new Flag("flag", "value");

        var act = () => FlagHasNoValue(flag, Name);

        act.Should().ThrowExactly<FlagException>();
    }

    [Fact]
    public void Tool_FlagHasNoValue_NoValue()
    {
        var flag = new Flag("flag", "");

        var act = () => FlagHasNoValue(flag, Name);

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
        var act = () => ValidateUrl(url, Name);
        var uri = ValidateUrl(url, Name);

        act.Should().NotThrow();
        uri.Should().BeAssignableTo(typeof(Uri));
    }

    [Theory]
    [InlineData("http:/google.com")]
    [InlineData("https//googlecom")]
    [InlineData("agaefafa")]
    public void Tool_ValidateUrl_Invalid(string url)
    {
        var act = () => ValidateUrl(url, Name);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Theory]
    [InlineData("afasfafaf")]
    [InlineData("C:/nowhere")]
    public void Tool_ValidatePath_Invalid(string path)
    {
        var act = () => ValidatePath(path, Name);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void Tool_ValidatePath_Valid()
    {
        var path = "";
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            path = @"C:\Users";
        if (Environment.OSVersion.Platform == PlatformID.Unix)
            path = "/home";

        var act = () => ValidatePath(path, Name);

        act.Should().NotThrow();
    }
}
