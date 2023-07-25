using Moq;

using FCli.Exceptions;
using FCli.Models.Tools;
using FCli.Services.Data;
using FCli.Services.Format;
using static FCli.Models.Args;

namespace FCli.Tests.Models.Tools;

public class RemoveTests : IDisposable
{
    private static readonly RemoveTool _testTool;

    private static readonly Mock<ICommandLoader> _fakeLoader;
    private static readonly Mock<ICommandLineFormatter> _fakeFormatter;

    static RemoveTests()
    {
        _fakeLoader = TestRepository.CommandLoaderFake;
        _fakeFormatter = TestRepository.FormatterFake;

        _testTool = new RemoveTool(
            _fakeFormatter.Object,
            _fakeLoader.Object);
    }

    [Fact]
    public void Remove_ShouldHandleHelp()
    {
        var act = () => _testTool.Action("", new List<Flag>
            {
                new Flag("help", "")
            });

        act.Should().NotThrow();
        _fakeFormatter.Verify(
            formatter => formatter.DisplayMessage(_testTool.Description),
            Times.Once);
    }

    [Fact]
    public void Remove_ShouldThrow_IfCommandIsUnknown()
    {
        var act = () => _testTool.Action("unknown", new());
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("all")]
    [InlineData("yes")]
    public void Remove_FlagsHaveNoValue(string flag)
    {
        var act = () => _testTool.Action(
            TestRepository.Command1.Name,
            new() { new Flag(flag, "value") });
        act.Should().Throw<FlagException>();
    }

    [Fact]
    public void Remove_ShouldThrow_IfUnknownFlag()
    {
        var act = () => _testTool.Action(
            TestRepository.Command1.Name,
            new() { new Flag("unknown", "") });
        act.Should().Throw<FlagException>();
    }

    [Fact]
    public void Remove_ShouldAvertDelete_IfAnyInput()
    {
        var act = () => _testTool.Action(TestRepository.Command1.Name, new());
        _fakeLoader.Invocations.Clear();
        _fakeFormatter
            .Setup(format => format.ReadUserInput("(yes/any)"))
            .Returns("any");

        act.Should().NotThrow();
        _fakeLoader.Verify(loader =>
            loader.DeleteCommand(TestRepository.Command1.Name),
            Times.Never);
        _fakeFormatter.Reset();
        _fakeFormatter
            .Setup(format => format.ReadUserInput("(yes/any)"))
            .Returns("yes");
    }

    [Fact]
    public void Remove_ShouldDelete_IfYesFlag()
    {
        var act = () => _testTool.Action(
            TestRepository.Command1.Name,
            new() { new Flag("yes", "") });
        _fakeLoader.Invocations.Clear();

        act.Should().NotThrow();
        _fakeLoader.Verify(loader =>
            loader.DeleteCommand(TestRepository.Command1.Name),
            Times.Once);
    }

    [Fact]
    public void Remove_ShouldDelete_IfConfirmed()
    {
        var act = () => _testTool.Action(TestRepository.Command3.Name, new());
        _fakeLoader.Invocations.Clear();

        act.Should().NotThrow();
        _fakeLoader.Verify(loader =>
            loader.DeleteCommand(TestRepository.Command3.Name),
            Times.Once);
    }

    [Fact]
    public void Remove_ShouldDeleteAll_IfConfirmed()
    {
        var act = () => _testTool.Action(
            "",
            new() { new Flag("all", "") });
        _fakeLoader.Invocations.Clear();

        act.Should().NotThrow();
        _fakeLoader.Verify(loader =>
            loader.DeleteCommand(TestRepository.Command1.Name), 
            Times.Once);
        _fakeLoader.Verify(loader =>
            loader.DeleteCommand(TestRepository.Command2.Name), 
            Times.Once);
        _fakeLoader.Verify(loader =>
            loader.DeleteCommand(TestRepository.Command3.Name), 
            Times.Once);
    }

    [Fact]
    public void Remove_ShouldAvertDeleteAll_IfConfirmed()
    {
        var act = () => _testTool.Action(
            "",
            new() { new Flag("all", "") });
        _fakeLoader.Invocations.Clear();
        _fakeFormatter
            .Setup(format => format.ReadUserInput("(yes/any)"))
            .Returns("any");

        act.Should().NotThrow();
        _fakeLoader.Verify(loader =>
            loader.DeleteCommand(TestRepository.Command1.Name),
            Times.Never);
        _fakeLoader.Verify(loader =>
            loader.DeleteCommand(TestRepository.Command2.Name),
            Times.Never);
        _fakeLoader.Verify(loader =>
            loader.DeleteCommand(TestRepository.Command3.Name),
            Times.Never);
        _fakeFormatter.Reset();
        _fakeFormatter
            .Setup(format => format.ReadUserInput("(yes/any)"))
            .Returns("yes");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Console.SetIn(Console.In);
    }
}