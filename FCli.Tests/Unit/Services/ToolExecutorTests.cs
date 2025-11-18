using FCli.Models.Types;
using FCli.Services;
using FCli.Services.Tools;
using FCli.Tests.Fixtures;

using Microsoft.Extensions.Logging.Abstractions;

namespace FCli.Tests.Unit.Services;

[Collection("Common")]
public class ToolExecutorTests
{
    private readonly ToolExecutor _testExecutor;
    private readonly ArgsParser _argsParser;

    public ToolExecutorTests(
        FormatterFixture formatter,
        ResourcesFixture resources)
    {
        _testExecutor = new ToolExecutor(
            new NullLogger<ToolExecutor>(),
            new List<ToolBase>() { new AddTool() });

        _argsParser = new ArgsParser(formatter.Object, resources.Object);
    }

    [Fact]
    public void GenericExecutor_ParseType_ShouldParse()
    {
        var args = _argsParser.ParseArgs(["add test"]);

        var type = _testExecutor.ParseType(args);

        type.Should().Be(ToolType.Add);
    }

    [Theory]
    [InlineData("")]
    [InlineData("args")]
    [InlineData("sel args")]
    public void GenericExecutor_ParseType_ShouldReturnNone(string cArgs)
    {
        var args = _argsParser.ParseArgs([cArgs]);

        var type = _testExecutor.ParseType(args);

        type.Should().Be(ToolType.None);
    }
}