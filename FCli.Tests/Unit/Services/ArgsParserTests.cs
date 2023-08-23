using FCli.Models;
using FCli.Services;
using FCli.Services.Abstractions;
using FCli.Tests.Fixtures;

namespace FCli.Tests.Unit.Services;

[Collection("Common")]
public class ArgsParserTests
{
    private readonly IArgsParser _argsParser;

    public ArgsParserTests(
        FormatterFixture formatter,
        ResourcesFixture resources)
    {
        _argsParser = new ArgsParser(formatter.Object, resources.Object);
    }

    [Fact]
    public void Args_Parse_EmptyString()
    {
        var cArgs = new string[] { string.Empty };

        var args = _argsParser.ParseArgs(cArgs);

        args.Should().NotBeNull().And.BeEquivalentTo(Args.None);
    }

    [Fact]
    public void Args_Parse_OneFlagNoValue()
    {
        var cArgs = new string[] { "--flag" };

        var args = _argsParser.ParseArgs(cArgs);

        args.Should().NotBeNull().And.NotBe(Args.None);
        args.Selector.Should().BeEmpty();
        args.Arg.Should().BeEmpty();

        args.Flags.Should()
            .ContainSingle(f => f.Key == "flag" && f.Value == "");
    }

    [Fact]
    public void Args_Parse_OneFlagValue()
    {
        var cArgs = new string[] { "--flag value" };

        var args = _argsParser.ParseArgs(cArgs);

        args.Should().NotBeNull().And.NotBe(Args.None);
        args.Selector.Should().BeEmpty();
        args.Arg.Should().BeEmpty();

        args.Flags.Should()
            .ContainSingle(f => f.Key == "flag"
                && f.Value == "value");
    }

    [Fact]
    public void Args_Parse_OneFlagValueSeparate()
    {
        var cArgs = new string[] { "--flag", "value" };

        var args = _argsParser.ParseArgs(cArgs);

        args.Should().NotBeNull().And.NotBe(Args.None);
        args.Selector.Should().BeEmpty();
        args.Arg.Should().BeEmpty();

        args.Flags.Should()
            .ContainSingle()
            .And.Contain(new Flag("flag", "value"));
    }

    [Fact]
    public void Args_Parse_MultipleFlagsValues()
    {
        var cArgs = new string[]
        {
            "--flag1", "value1", "--flag2", "value2"
        };

        var args = _argsParser.ParseArgs(cArgs);

        args.Should().NotBeNull().And.NotBe(Args.None);
        args.Selector.Should().BeEmpty();
        args.Arg.Should().BeEmpty();

        args.Flags.Should().HaveCount(2)
            .And.ContainInOrder(
            new Flag("flag1", "value1"),
            new Flag("flag2", "value2"));
    }

    [Fact]
    public void Args_Parse_SelectorNoFlags()
    {
        var cArgs = new string[] { "selector" };


        var args = _argsParser.ParseArgs(cArgs);

        args.Should().NotBeNull().And.NotBe(Args.None);
        args.Arg.Should().BeEmpty();
        args.Flags.Should().BeEmpty();

        args.Selector.Should().Be("selector");
    }

    [Fact]
    public void Args_Parse_SelectorFlag()
    {
        var cArgs = new string[] { "selector --flag" };

        var args = _argsParser.ParseArgs(cArgs);

        args.Should().NotBeNull().And.NotBe(Args.None);
        args.Arg.Should().BeEmpty();

        args.Selector.Should().Be("selector");
        args.Flags.Should().ContainSingle()
            .And.Contain(new Flag("flag", ""));
    }

    [Fact]
    public void Args_Parse_SelectorArg()
    {
        var cArgs = new string[] { "selector", "arg" };

        var args = _argsParser.ParseArgs(cArgs);

        args.Should().NotBeNull().And.NotBe(Args.None);
        args.Flags.Should().BeEmpty();

        args.Selector.Should().Be("selector");
        args.Arg.Should().Be("arg");
    }

    [Fact]
    public void Args_Parse_SelectorArgFlags()
    {
        var cArgs = new string[]
        {
            "selector", "arg", "--flag1", "value1", "--flag2", "value2"
        };

        var args = _argsParser.ParseArgs(cArgs);

        args.Should().NotBeNull().And.NotBe(Args.None);

        args.Selector.Should().Be("selector");
        args.Arg.Should().Be("arg");
        args.Flags.Should().HaveCount(2)
            .And.ContainInOrder(
            new Flag("flag1", "value1"),
            new Flag("flag2", "value2"));
    }

    [Fact]
    public void Args_Parse_InvalidNumberOfArgs()
    {
        var args = new string[]
        {
            "selector", "arg", "arg2", "arg3"
        };

        var act = () => _argsParser.ParseArgs(args);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void Args_Parse_QuotedArg()
    {
        var cArgs = new string[]
        {
            "selector", @"/path/to/arg/",
        };

        var args = _argsParser.ParseArgs(cArgs);

        args.Selector.Should().Be("selector");
        args.Arg.Should().Be("/path/to/arg/");
    }

    [Fact]
    public void Args_Parse_QuotedArgWithFlag()
    {
        var cArgs = new string[]
        {
            "selector \"/path to/arg/\" --flag value"
        };

        var args = _argsParser.ParseArgs(cArgs);

        args.Selector.Should().Be("selector");
        args.Arg.Should().Be("/path to/arg/");
        args.Flags.Should().HaveCount(1)
            .And.Contain(new Flag("flag", "value"));
    }

    [Fact]
    public void Args_Parse_PathArg()
    {
        var cArgs = new string[]
        {
            "selector", "/path to/arg/"
        };

        var args = _argsParser.ParseArgs(cArgs);

        args.Selector.Should().Be("selector");
        args.Arg.Should().Be("/path to/arg/");
    }
}