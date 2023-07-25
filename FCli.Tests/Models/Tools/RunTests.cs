using Moq;

using FCli.Models;
using FCli.Exceptions;
using FCli.Models.Tools;
using static FCli.Models.Args;
using FCli.Services;
using FCli.Services.Format;

namespace FCli.Tests.Models.Tools;

public class RunTests
{
    private static readonly RunTool _testTool;
    
    private static readonly Mock<IToolExecutor> _fakeExecutor;
    private static readonly Mock<ICommandFactory> _fakeFactory;
    private static readonly Mock<ICommandLineFormatter> _fakeFormatter;

    static RunTests()
    {
        _fakeExecutor = TestRepository.ToolExecutorFake;
        _fakeFactory = TestRepository.CommandFactoryFake;
        _fakeFormatter = TestRepository.FormatterFake;

        _testTool = new RunTool(
            _fakeFormatter.Object,
            _fakeExecutor.Object,
            _fakeFactory.Object);
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
    [InlineData("exe", "", CommandType.Executable)]
    [InlineData("url", "", CommandType.Url)]
    [InlineData("script", "bash", CommandType.Bash)]
    [InlineData("script", "cmd", CommandType.CMD)]
    [InlineData("script", "powershell", CommandType.Powershell)]
    [InlineData("script", "none", CommandType.None)]
    public void Run_ParseTypeFlags(
        string flag,
        string value,
        CommandType commandType)
    {
        var path = commandType == CommandType.Url 
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
                commandType == CommandType.Url ? "" : "option"), Times.Once);
        }
        else act.Should().Throw<FlagException>();
    }
}