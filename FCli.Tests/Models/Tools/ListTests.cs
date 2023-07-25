using FCli.Exceptions;
using FCli.Models.Tools;
using FCli.Services;
using FCli.Services.Data;
using FCli.Services.Format;
using Moq;
using static FCli.Models.Args;

namespace FCli.Tests.Models.Tools;

public class ListTests
{
    private static readonly ListTool _testTool;

    private static readonly Mock<IToolExecutor> _fakeExecutor;
    private static readonly Mock<ICommandLoader> _fakeLoader;
    private static readonly Mock<ICommandLineFormatter> _fakeFormatter;

    static ListTests()
    {
        _fakeExecutor = TestRepository.ToolExecutorFake;
        _fakeLoader = TestRepository.CommandLoaderFake;
        _fakeFormatter = TestRepository.FormatterFake;

        _testTool = new ListTool(
            _fakeFormatter.Object,
            _fakeExecutor.Object,
            _fakeLoader.Object);
    }

    [Fact]
    public void List_HandleHelp()
    {
        var act = () => _testTool.Action("", new List<Flag>() { new Flag("help", "") });
        
        act.Should().NotThrow();
        _fakeFormatter.Verify(formatter => 
            formatter.DisplayMessage(_testTool.Description), Times.Once);
    }

    [Fact]
    public void List_NoFlags_DisplayAll()
    {
        var act = () => _testTool.Action("", new List<Flag>());
        _fakeLoader.Invocations.Clear();
        
        act.Should().NotThrow();
        _fakeLoader.Verify(loader => loader.LoadCommands(), Times.Once);
    }

    [Theory]
    [InlineData("url")]
    [InlineData("exe")]
    [InlineData("tool")]
    [InlineData("script")]
    public void List_FlagsHaveNoValue(string flag)
    {
        var act = () => _testTool.Action("", new List<Flag>() { new Flag(flag, "value") });

        act.Should().Throw<FlagException>();
    }

    [Theory]
    [InlineData("script")]
    [InlineData("url")]
    [InlineData("exe")]
    [InlineData("tool")]
    public void List_ParseFlags(string flag)
    {
        var act = () => _testTool.Action("", new List<Flag>() { new Flag(flag, "") });
        act.Should().NotThrow();
        if (flag == "tool")
            _fakeExecutor.VerifyGet(executor => executor.KnownTools);
    }

    [Fact]
    public void List_ShouldThrow_IfUnknownFlag()
    {
        var act = () => _testTool.Action("", new List<Flag>() { new Flag("unknown", "") });
        act.Should().Throw<FlagException>();
    }

    [Fact]
    public void List_Arg_ShouldAcceptAsFilter()
    {
        var act = () => _testTool.Action("filter", new List<Flag>() { });
        act.Should().NotThrow();
    }
}
