using Moq;

using FCli.Models;
using FCli.Exceptions;
using FCli.Models.Tools;
using static FCli.Models.Args;
using FCli.Services;
using FCli.Services.Format;
using System.Resources;
using FCli.Services.Config;
using FCli.Models.Types;

namespace FCli.Tests.Models.Tools;

public class RunTests
{
    private static readonly RunTool _testTool;
    
    private static readonly Mock<ICommandFactory> _fakeFactory;
    private static readonly Mock<ICommandLineFormatter> _fakeFormatter;
    private static readonly Mock<ResourceManager> _fakeResources;
    private static readonly Mock<IConfig> _fakeConfig;

    static RunTests()
    {
        _fakeFactory = TestRepository.CommandFactoryFake;
        _fakeFormatter = TestRepository.FormatterFake;
        _fakeResources = TestRepository.ResourcesFake;
        _fakeConfig = TestRepository.ConfigFake;

        _testTool = new RunTool(
            _fakeFormatter.Object,
            _fakeResources.Object,
            _fakeFactory.Object,
            _fakeConfig.Object);
    }

    [Fact]
    public void Run_ShouldHandleHelp()
    {
        var act = () => _testTool.Action("", new() { new Flag("help", "") });

        act.Should().NotThrow();
        _fakeFormatter.Verify(
            formatter => formatter.DisplayMessage(_testTool.Description),
            Times.Once);
    }

    [Fact]
    public void Run_ShouldThrow_IfNoArg()
    {
        var act = () => _testTool.Action("", new());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Run_ShouldThrow_IfNoTypeFlags()
    {
        var act = () => _testTool.Action("command", new());
        act.Should().Throw<FlagException>();
    }

    [Fact]
    public void Run_ShouldThrow_IfMultipleTypeArgs()
    {
        var act = () => _testTool.Action("arg", new() {
            new Flag("exe", ""),
            new Flag("url", "")
        });
        act.Should().Throw<FlagException>();
    }

    [Theory]
    [InlineData("exe")]
    [InlineData("url")]
    public void Run_FlagsHaveNoValue(string flag)
    {
        var act = () => _testTool.Action("arg", new()
        {
            new Flag(flag, "value")
        });
        act.Should().Throw<FlagException>();
    }

    [Theory]
    [InlineData("script")]
    [InlineData("options")]
    public void Run_FlagsHaveValue(string flag)
    {
        var act = () => _testTool.Action("arg", new()
        {
            new Flag(flag, "")
        });
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
            : Path.Combine(TestRepository.TestFilesPath, TestRepository.ExecutableName);
        var act = () => _testTool.Action(path, new()
        {
            new Flag(flag, value),
            new Flag("options", "option")
        });
        _fakeFactory.Invocations.Clear();

        if (commandType != CommandType.None)
        {
            act.Should().Throw<CriticalException>();
            _fakeFactory.Verify(factory => factory.Construct(
                "runner",
                path,
                commandType,
                shellType,
                commandType == CommandType.Website ? "" : "option"), Times.Once);
        }
        else act.Should().Throw<FlagException>();
    }
}