using Microsoft.Extensions.Logging.Abstractions;
using Moq;

using FCli.Services;
using FCli.Models;
using FCli.Exceptions;
using FCli.Services.Data;
using FCli.Services.Format;
using FCli.Models.Types;
using FCli.Services.Config;
using System.Resources;

namespace FCli.Tests.Services;

public class ToolExecutorTests
{
    private static readonly ToolExecutor _testExecutor;

    private static readonly Mock<ICommandLoader> _fakeLoader;
    private static readonly Mock<ICommandFactory> _fakeFactory;
    private static readonly Mock<ICommandLineFormatter> _fakeFormatter;
    private static readonly Mock<IConfig> _fakeConfig;
    private static readonly Mock<ResourceManager> _fakeResources;

    static ToolExecutorTests()
    {
        _fakeLoader = TestRepository.CommandLoaderFake;
        _fakeFactory = TestRepository.CommandFactoryFake;
        _fakeFormatter = TestRepository.FormatterFake;
        _fakeConfig = TestRepository.ConfigFake;
        _fakeResources = TestRepository.ResourcesFake;
        
        _testExecutor = new ToolExecutor(
            _fakeLoader.Object,
            NullLogger<ToolExecutor>.Instance,
            _fakeFactory.Object,
            _fakeFormatter.Object,
            _fakeConfig.Object,
            _fakeResources.Object);
    }

    [Fact]
    public void GenericExecutor_Create()
    {
        _testExecutor.Tools.Should().NotBeNullOrEmpty();
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