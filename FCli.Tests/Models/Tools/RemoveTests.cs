using System.Globalization;
using FCli.Common.Exceptions;
using FCli.Models.Tools;
using FCli.Services.Data;
using Moq;
using static FCli.Models.Args;

namespace FCli.Tests.Models.Tools;

public class RemoveTests : IDisposable
{
    private static readonly RemoveTool _testTool;

    private static readonly Mock<ICommandLoader> _fakeLoader;

    static RemoveTests()
    {
        _fakeLoader = TestRepository.CommandLoaderFake;

        _testTool = new RemoveTool(_fakeLoader.Object);
    }

    [Fact]
    public void Remove_ShouldHandleHelp()
    {
        var act = () => _testTool.Action(
            "",
            new List<Flag> { new Flag("help", "") });
        act.Should().NotThrow();
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
            TestRepository.TestCommand.Name,
            new() { new Flag(flag, "value") });
        act.Should().Throw<FlagException>();
    }
    
    [Fact]
    public void Remove_ShouldThrow_IfUnknownFlag()
    {
        var act = () => _testTool.Action(
            TestRepository.TestCommand.Name,
            new() { new Flag("unknown", "") });
        act.Should().Throw<FlagException>();
    }

    [Fact]
    public void Remove_ShouldAvertDelete_IfAnyInput()
    {
        var act = () => _testTool.Action(TestRepository.TestCommand.Name, new());
        Console.SetIn(new StringReader("any"));
        _fakeLoader.Invocations.Clear();

        act.Should().NotThrow();
        _fakeLoader.Verify(loader =>
            loader.DeleteCommand(TestRepository.TestCommand.Name), 
            Times.Never);
    }

    [Fact]
    public void Remove_ShouldDelete_IfYesFlag()
    {
        var act = () => _testTool.Action(
            TestRepository.TestCommand.Name,
            new() { new Flag("yes", "") });

        _fakeLoader.Invocations.Clear();

        act.Should().NotThrow();
        _fakeLoader.Verify(loader =>
            loader.DeleteCommand(TestRepository.TestCommand.Name), 
            Times.Once);
    }

    [Fact]
    public void Remove_ShouldDelete_IfConfirmed()
    {
        var act = () => _testTool.Action(TestRepository.TestCommand.Name, new());
        Console.SetIn(new StringReader("yes"));

        _fakeLoader.Invocations.Clear();

        act.Should().NotThrow();
        _fakeLoader.Verify(loader =>
            loader.DeleteCommand(TestRepository.TestCommand.Name), 
            Times.Once);
    }

    [Fact]
    public void Remove_ShouldDeleteAll_IfConfirmed()
    {
        var act = () => _testTool.Action(
            TestRepository.TestCommand.Name,
            new() { new Flag("all", "") });
        Console.SetIn(new StringReader("yes"));

        _fakeLoader.Invocations.Clear();

        act.Should().NotThrow();
        _fakeLoader.Verify(loader =>
            loader.DeleteCommand(TestRepository.TestCommand.Name));
    }

    [Fact]
    public void Remove_ShouldAvertDeleteAll_IfConfirmed()
    {
        var act = () => _testTool.Action(
            TestRepository.TestCommand.Name,
            new() { new Flag("all", "") });
        Console.SetIn(new StringReader("any"));

        _fakeLoader.Invocations.Clear();

        act.Should().NotThrow();
        _fakeLoader.Verify(loader =>
            loader.DeleteCommand(TestRepository.TestCommand.Name), 
            Times.Never);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Console.SetIn(Console.In);
    }
}