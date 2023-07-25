using Microsoft.Extensions.Logging.Abstractions;

using FCli.Services;
using FCli.Models;
using FCli.Common.Exceptions;
using Moq;
using FCli.Services.Data;

namespace FCli.Tests.Services;

public class GenericExecutorTests
{
    private static readonly GenericExecutor _testExecutor;

    private static readonly Mock<ICommandLoader> _fakeLoader;
    private static readonly Mock<ICommandFactory> _fakeFactory;

    static  GenericExecutorTests()
    {
        _fakeLoader = TestRepository.CommandLoaderFake;
        _fakeFactory = TestRepository.CommandFactoryFake;
        
        _testExecutor = new GenericExecutor(
            _fakeLoader.Object,
            NullLogger<GenericExecutor>.Instance,
            _fakeFactory.Object);
    }

    [Fact]
    public void GenericExecutor_Create()
    {
        _testExecutor.KnownTools.Should().NotBeNullOrEmpty();
        _testExecutor.KnownTypeFlags.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenericExecutor_Execute()
    {
        var args = Args.Parse(new string[] { "list", "--exe" });
        var act = () => _testExecutor.Execute(args, _testExecutor.ParseType(args));

        act.Should().NotThrow();
    }

    [Fact]
    public void GenericExecutor_Execute_BadToolType()
    {
        var act = () => _testExecutor.Execute(Args.None, ToolType.None);

        act.Should().Throw<CriticalException>();
    }

    [Fact]
    public void GenericExecutor_Execute_ShouldHandleArgs()
    {
        var act = () => _testExecutor.Execute(Args.None, ToolType.Add);

        act.Should().NotThrow();
    }

    [Fact]
    public void GenericExecutor_ParseType_ShouldParse()
    {
        var args = Args.Parse(new string[] { "add test" });

        var type = _testExecutor.ParseType(args);

        type.Should().Be(ToolType.Add);
    }

    [Theory]
    [InlineData("")]
    [InlineData("args")]
    [InlineData("sel args")]
    public void GenericExecutor_ParseType_ShouldReturnNone(string args)
    {
        var type = _testExecutor.ParseType(Args.Parse(new string[] { args }));

        type.Should().Be(ToolType.None);
    }
}