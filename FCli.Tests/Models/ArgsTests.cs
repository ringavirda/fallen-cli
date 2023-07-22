using FCli.Models;

namespace FCli.Tests.Models;

public class ArgsTests
{
    [Fact]
    public void Args_Parse_EmptyString()
    {
        var args = new string[] { string.Empty };

        var aargs = Args.Parse(args);

        aargs.Should().NotBeNull().And.BeEquivalentTo(Args.None);
    }

    [Fact]
    public void Args_Parse_OneFlagNoValue()
    {
        var args = new string[] { "--flag" };

        var aargs = Args.Parse(args);

        aargs.Should().NotBeNull().And.NotBe(Args.None);
        aargs.Selector.Should().BeEmpty();
        aargs.Arg.Should().BeEmpty();

        aargs.Flags.Should()
            .ContainSingle(f => f.Key == "flag" && f.Value == "");
    }

    [Fact]
    public void Args_Parse_OneFlagValue()
    {
        var args = new string[] { "--flag value" };

        var aargs = Args.Parse(args);

        aargs.Should().NotBeNull().And.NotBe(Args.None);
        aargs.Selector.Should().BeEmpty();
        aargs.Arg.Should().BeEmpty();

        aargs.Flags.Should()
            .ContainSingle(f => f.Key == "flag"
                && f.Value == "value");
    }

    [Fact]
    public void Args_Parse_OneFlagValueSeparate()
    {
        var args = new string[] { "--flag", "value" };

        var aargs = Args.Parse(args);

        aargs.Should().NotBeNull().And.NotBe(Args.None);
        aargs.Selector.Should().BeEmpty();
        aargs.Arg.Should().BeEmpty();

        aargs.Flags.Should()
            .ContainSingle()
            .And.Contain(new Flag("flag", "value"));
    }

    [Fact]
    public void Args_Parse_MultipleFlagsValues()
    {
        var args = new string[]
        {
            "--flag1 value1", "--flag2", "value2"
        };

        var aargs = Args.Parse(args);

        aargs.Should().NotBeNull().And.NotBe(Args.None);
        aargs.Selector.Should().BeEmpty();
        aargs.Arg.Should().BeEmpty();

        aargs.Flags.Should().HaveCount(2)
            .And.ContainInOrder(
            new Flag("flag1", "value1"),
            new Flag("flag2", "value2"));
    }

    [Fact]
    public void Args_Parse_SelectorNoFlags()
    {
        var args = new string[] { "selector" };


        var aargs = Args.Parse(args);

        aargs.Should().NotBeNull().And.NotBe(Args.None);
        aargs.Arg.Should().BeEmpty();
        aargs.Flags.Should().BeEmpty();

        aargs.Selector.Should().Be("selector");
    }

    [Fact]
    public void Args_Parse_SelectorFlag()
    {
        var args = new string[] { "selector --flag" };


        var aargs = Args.Parse(args);

        aargs.Should().NotBeNull().And.NotBe(Args.None);
        aargs.Arg.Should().BeEmpty();

        aargs.Selector.Should().Be("selector");
        aargs.Flags.Should().ContainSingle()
            .And.Contain(new Flag("flag", ""));
    }

    [Fact]
    public void Args_Parse_SelectorArg()
    {
        var args = new string[] { "selector", "arg" };

        var aargs = Args.Parse(args);

        aargs.Should().NotBeNull().And.NotBe(Args.None);
        aargs.Flags.Should().BeEmpty();

        aargs.Selector.Should().Be("selector");
        aargs.Arg.Should().Be("arg");
    }

    [Fact]
    public void Args_Parse_SelectorArgFlags()
    {
        var args = new string[]
        {
            "selector", "arg", "--flag1 value1", "--flag2", "value2"
        };

        var aargs = Args.Parse(args);

        aargs.Should().NotBeNull().And.NotBe(Args.None);

        aargs.Selector.Should().Be("selector");
        aargs.Arg.Should().Be("arg");
        aargs.Flags.Should().HaveCount(2)
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

        var act = () => Args.Parse(args);

        act.Should().ThrowExactly<ArgumentException>();
    }
}