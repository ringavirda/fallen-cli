using FCli.Exceptions;
using FCli.Models;
using FCli.Services.Tools;
using FCli.Tests.Fixtures;

using Moq;

namespace FCli.Tests.Unit.Services.Tools;

[Collection("Common")]
public class RemoveToolTests
    : IClassFixture<LoaderFixture>, IClassFixture<FactoryFixture>
{
    private readonly RemoveTool _testTool;
    private readonly FormatterFixture _formatter;
    private readonly LoaderFixture _loader;
    private readonly FactoryFixture _factory;

    public RemoveToolTests(
        FormatterFixture formatter,
        ResourcesFixture resources,
        LoaderFixture loader,
        FactoryFixture factory)
    {
        _testTool = new RemoveTool(
            formatter.Object,
            resources.Object,
            loader.Object);
        _formatter = formatter;
        _loader = loader;
        _factory = factory;

        _formatter.Invocations.Clear();
        _loader.Invocations.Clear();
    }

    [Fact]
    public void Remove_ShouldHandleHelp()
    {
        var act = () => _testTool.Execute("", [new Flag("help", "")]);

        act.Should().NotThrow();
        _formatter.Verify(
            formatter => formatter.DisplayMessage(_testTool.Description),
            Times.Once);
    }

    [Fact]
    public void Remove_ShouldThrow_IfCommandIsUnknown()
    {
        var act = () => _testTool.Execute("unknown", []);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("all")]
    [InlineData("yes")]
    public void Remove_FlagsHaveNoValue(string flag)
    {
        var act = () => _testTool.Execute(
            _factory.Command1.Name, [new Flag(flag, "value")]);

        act.Should().Throw<FlagException>();
    }

    [Fact]
    public void Remove_ShouldThrow_IfUnknownFlag()
    {
        var act = () => _testTool.Execute(
            _factory.Command1.Name, [new Flag("unknown", "")]);

        act.Should().Throw<FlagException>();
    }

    [Fact]
    public void Remove_ShouldAvertDelete_IfAnyInput()
    {
        _formatter
            .Setup(format => format.ReadUserInput("(yes/any)", false))
            .Returns("any");
        var act = () => _testTool.Execute(_factory.Command1.Name, []);

        act.Should().NotThrow();
        _loader.Verify(loader =>
            loader.DeleteCommand(_factory.Command1.Name),
            Times.Never);
        _formatter.Reset();
    }

    [Fact]
    public void Remove_ShouldDelete_IfYesFlag()
    {
        var act = () => _testTool.Execute(
            _factory.Command1.Name, [new Flag("yes", "")]);

        act.Should().NotThrow();
        _loader.Verify(loader =>
            loader.DeleteCommand(_factory.Command1.Name),
            Times.Once);
    }

    [Fact]
    public void Remove_ShouldDelete_IfConfirmed()
    {
        _formatter
            .Setup(format => format.ReadUserInput("(yes/any)", false))
            .Returns("yes");
        var act = () => _testTool.Execute(_factory.Command3.Name, []);

        act.Should().NotThrow();
        _loader.Verify(loader =>
            loader.DeleteCommand(_factory.Command3.Name),
            Times.Once);
        _formatter.Reset();
    }

    [Fact]
    public void Remove_ShouldDeleteAll_IfConfirmed()
    {
        _formatter
            .Setup(format => format.ReadUserInput("(yes/any)", false))
            .Returns("yes");
        var act = () => _testTool.Execute("", [new Flag("all", "")]);

        act.Should().NotThrow();
        _loader.Verify(loader =>
            loader.DeleteCommand(_factory.Command1.Name),
            Times.Once);
        _loader.Verify(loader =>
            loader.DeleteCommand(_factory.Command2.Name),
            Times.Once);
        _loader.Verify(loader =>
            loader.DeleteCommand(_factory.Command3.Name),
            Times.Once);
        _formatter.Reset();
    }

    [Fact]
    public void Remove_ShouldNotDeleteAll_IfNotConfirmed()
    {
        _formatter
            .Setup(format => format.ReadUserInput("(yes/any)", false))
            .Returns("any");
        var act = () => _testTool.Execute("", [new Flag("all", "")]);

        act.Should().NotThrow();
        _loader.Verify(loader =>
            loader.DeleteCommand(_factory.Command1.Name),
            Times.Never);
        _loader.Verify(loader =>
            loader.DeleteCommand(_factory.Command2.Name),
            Times.Never);
        _loader.Verify(loader =>
            loader.DeleteCommand(_factory.Command3.Name),
            Times.Never);
        _formatter.Reset();
    }
}