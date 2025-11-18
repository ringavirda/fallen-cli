using FCli.Exceptions;
using FCli.Models;
using FCli.Services.Tools;
using FCli.Tests.Fixtures;

using Moq;

namespace FCli.Tests.Unit.Services.Tools;

[Collection("Common")]
public class ListToolTests
    : IClassFixture<ConfigFixture>, IClassFixture<LoaderFixture>
{
    private readonly ListTool _testTool;
    private readonly FormatterFixture _formatter;
    private readonly ConfigFixture _config;
    private readonly LoaderFixture _loader;

    public ListToolTests(
        FormatterFixture formatter,
        ResourcesFixture resources,
        ConfigFixture config,
        LoaderFixture loader)
    {
        _testTool = new ListTool(
            formatter.Object,
            resources.Object,
            config.Object,
            loader.Object);
        _formatter = formatter;
        _config = config;
        _loader = loader;

        _formatter.Invocations.Clear();
        _loader.Invocations.Clear();
    }

    [Fact]
    public void List_HandleHelp()
    {
        var act = () => _testTool.Execute("", [new Flag("help", "")]);

        act.Should().NotThrow();
        _formatter.Verify(formatter =>
            formatter.DisplayMessage(_testTool.Description), Times.Once);
    }

    [Fact]
    public void List_NoFlags_DisplayAll()
    {
        var act = () => _testTool.Execute("", []);
        _loader.Invocations.Clear();

        act.Should().NotThrow();
        _loader.Verify(loader => loader.LoadCommands(), Times.Once);
    }

    [Theory]
    [InlineData("url")]
    [InlineData("exe")]
    [InlineData("tool")]
    [InlineData("script")]
    public void List_FlagsHaveNoValue(string flag)
    {
        var act = () => _testTool.Execute("", [new Flag(flag, "value")]);

        act.Should().Throw<FlagException>();
    }

    [Theory]
    [InlineData("script")]
    [InlineData("url")]
    [InlineData("exe")]
    [InlineData("tools")]
    public void List_ParseFlags(string flag)
    {
        var act = () => _testTool.Execute("", [new Flag(flag, "")]);

        act.Should().NotThrow();
        if (flag == "tool")
            _config.VerifyGet(cnf => cnf.KnownTools);
        else
            _loader.Verify(loader => loader.LoadCommands(), Times.Once);
    }

    [Fact]
    public void List_ShouldThrow_IfUnknownFlag()
    {
        var act = () => _testTool.Execute("", [new Flag("unknown", "")]);

        act.Should().Throw<FlagException>();
    }

    [Fact]
    public void List_Arg_ShouldAcceptAsFilter()
    {
        var act = () => _testTool.Execute("filter", []);

        act.Should().NotThrow();
    }
}