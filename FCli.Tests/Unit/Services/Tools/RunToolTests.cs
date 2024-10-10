using FCli.Exceptions;
using FCli.Models;
using FCli.Models.Dtos;
using FCli.Models.Types;
using FCli.Services.Tools;
using FCli.Tests.Fixtures;

using Moq;

namespace FCli.Tests.Unit.Services.Tools;

[Collection("Common")]
public class RunToolTests
    : IClassFixture<ConfigFixture>, IClassFixture<FactoryFixture>
{
    private readonly RunTool _testTool;
    private readonly FormatterFixture _formatter;
    private readonly ConfigFixture _config;
    private readonly FactoryFixture _factory;

    public RunToolTests(
        FormatterFixture formatter,
        ResourcesFixture resources,
        ConfigFixture config,
        FactoryFixture factory)
    {
        _testTool = new RunTool(
            formatter.Object,
            resources.Object,
            config.Object,
            factory.Object);
        _formatter = formatter;
        _config = config;
        _factory = factory;

        _factory.Invocations.Clear();
    }

    [Fact]
    public void Run_ShouldHandleHelp()
    {
        var act = () => _testTool.Execute("", [new Flag("help", "")]);

        act.Should().NotThrow();
        _formatter.Verify(
            formatter => formatter.DisplayMessage(_testTool.Description),
            Times.Once);
    }

    [Fact]
    public void Run_ShouldThrow_IfNoArg()
    {
        var act = () => _testTool.Execute("", []);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Run_ShouldThrow_IfNoTypeFlags()
    {
        var act = () => _testTool.Execute("command", []);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Run_ShouldThrow_IfMultipleTypeArgs()
    {
        var act = () => _testTool.Execute("arg",
            [
                new Flag("exe", ""),
                new Flag("url", "")
            ]);

        act.Should().Throw<FlagException>();
    }

    [Theory]
    [InlineData("exe")]
    [InlineData("url")]
    public void Run_FlagsHaveNoValue(string flag)
    {
        var act = () => _testTool.Execute("arg", [new Flag(flag, "value")]);

        act.Should().Throw<FlagException>();
    }

    [Theory]
    [InlineData("script")]
    [InlineData("options")]
    public void Run_FlagsHaveValue(string flag)
    {
        var act = () => _testTool.Execute("arg", [new Flag(flag, "")]);

        act.Should().Throw<FlagException>();
    }

    [Theory]
    [InlineData("exe", "", CommandType.Executable, ShellType.None)]
    [InlineData("url", "", CommandType.Website, ShellType.None)]
    [InlineData("script", "bash", CommandType.Script, ShellType.Bash)]
    [InlineData("script", "cmd", CommandType.Script, ShellType.Cmd)]
    [InlineData("script", "powershell", CommandType.Script, ShellType.Powershell)]
    [InlineData("script", "none", CommandType.Script, ShellType.None)]
    public void Run_ParseTypeFlags(
        string flag,
        string value,
        CommandType commandType,
        ShellType shellType)
    {
        var path = commandType == CommandType.Website
            ? "https://google.com/"
            : Path.Combine(_config.TestFilesPath, _config.ExecutableName);
        var act = () => _testTool.Execute(path,
            [
                new Flag(flag, value),
                new Flag("options", "option")
            ]);

        if (!(commandType == CommandType.Script && shellType == ShellType.None))
        {
            act.Should().Throw<CriticalException>();
            _factory.Verify(factory =>
                factory.Construct(It.Is<CommandAlterRequest>(req =>
                    req.Name == "runner"
                    && req.Path == path
                    && req.Type == commandType
                    && req.Shell == shellType)),
                Times.Once);
        }
        else act.Should().Throw<ArgumentException>();
    }
}