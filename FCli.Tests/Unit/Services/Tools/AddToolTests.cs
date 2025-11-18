using FCli.Exceptions;
using FCli.Models;
using FCli.Models.Dtos;
using FCli.Models.Types;
using FCli.Services.Tools;
using FCli.Tests.Fixtures;

using Moq;

namespace FCli.Tests.Unit.Services.Tools;

[Collection("Common")]
public class AddToolTests :
    IClassFixture<ConfigFixture>,
    IClassFixture<LoaderFixture>,
    IClassFixture<FactoryFixture>
{
    private readonly AddTool _testTool;
    private readonly FormatterFixture _formatter;
    private readonly ConfigFixture _config;
    private readonly LoaderFixture _loader;
    private readonly FactoryFixture _factory;

    public AddToolTests(
        FormatterFixture formatter,
        ResourcesFixture resources,
        ConfigFixture config,
        LoaderFixture loader,
        FactoryFixture factory)
    {
        _testTool = new AddTool(
            formatter.Object,
            resources.Object,
            config.Object,
            loader.Object,
            factory.Object);
        _formatter = formatter;
        _config = config;
        _loader = loader;
        _factory = factory;

        _formatter.Invocations.Clear();
        _loader.Invocations.Clear();
        _factory.Invocations.Clear();

        _formatter.Setup(format => format.ReadUserInput("(yes/any)", false))
            .Returns("yes");
    }

    [Fact]
    public void Add_HandleHelp()
    {
        var act = () => _testTool.Execute("", [new Flag("help", "")]);

        act.Should().NotThrow();
        _formatter.Verify(formatter =>
            formatter.DisplayMessage(_testTool.Description), Times.Once);
    }

    [Fact]
    public void Add_ShouldHaveArg()
    {
        var act = () => _testTool.Execute("", [new Flag("flag", "")]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_ShouldHaveOnlyOneTypeFlag()
    {
        var act = () => _testTool.Execute("test",
            [
                new Flag("exe", ""),
                new Flag("url", "")
            ]);

        act.Should().Throw<FlagException>();
    }

    [Fact]
    public void Add_ParseFlags_Name()
    {
        var name = "testName";
        var path = Path.Combine(_config.TestFilesPath, _config.BashScriptName);

        _testTool.Execute(path, [new Flag("name", name)]);

        _factory.Verify(factory => factory.Construct(
            It.Is<CommandAlterRequest>(req =>
                req.Name == name
                && req.Path == path
                && req.Type == CommandType.Script
                && req.Shell == ShellType.Bash)),
            Times.Once);
    }

    [Fact]
    public void Add_ParseFlags_Options()
    {
        var options = "testOptions";
        var path = Path.Combine(_config.TestFilesPath, _config.BashScriptName);

        _testTool.Execute(path, [new Flag("options", options)]);

        var commandName = _config.BashScriptName.Split('.')[0];
        _factory.Verify(factory => factory.Construct(
            It.Is<CommandAlterRequest>(req =>
                req.Name == commandName
                && req.Type == CommandType.Script
                && req.Shell == ShellType.Bash)),
            Times.Once);
    }

    [Fact]
    public void Add_ParseFlags_NameAndOptions()
    {
        var name = "testName";
        var options = "testOptions";
        var path = Path.Combine(_config.TestFilesPath, _config.BashScriptName);

        _testTool.Execute(path,
            [
                new Flag("name", name),
                new Flag("options", options)
            ]);

        _factory.Verify(factory => factory.Construct(
            It.Is<CommandAlterRequest>(req =>
                req.Name == name
                && req.Path == path
                && req.Type == CommandType.Script
                && req.Shell == ShellType.Bash
                && req.Options == options)),
            Times.Once);
    }

    [Theory]
    [InlineData("exe", "", CommandType.Executable, ShellType.None)]
    [InlineData("url", "", CommandType.Website, ShellType.None)]
    [InlineData("script", "bash", CommandType.Script, ShellType.Bash)]
    [InlineData("script", "powershell", CommandType.Script, ShellType.Powershell)]
    [InlineData("script", "cmd", CommandType.Script, ShellType.Cmd)]
    public void Add_ParseFlags_TypeFlags(
        string flag,
        string value,
        CommandType commandType,
        ShellType shellType)
    {
        var name = "testName1";
        var options = "testOptions1";
        string path = flag == "url"
            ? "https://somwhere.com"
            : Path.Combine(_config.TestFilesPath, _config.PSScriptName);

        var act = () => _testTool.Execute(path,
            [
                new Flag("name", name),
                new Flag("options", options),
                new Flag(flag, value)
            ]);

        if (Environment.OSVersion.Platform == PlatformID.Unix
            && commandType == CommandType.Script
            && shellType == ShellType.Cmd)
        {
            act.Should().Throw<ArgumentException>();
            return;
        }
        act();

        _factory.Verify(factory => factory.Construct(
            It.Is<CommandAlterRequest>(req =>
                req.Name == name
                && req.Path == path
                && req.Type == commandType
                && req.Shell == shellType
                && req.Options == options)),
            Times.Once);
    }

    [Fact]
    public void Add_ParseFlags_TypeFlagsShouldThrowIfUnknown()
    {
        var name = "testName";
        var options = "testOptions";
        var path = "https://somwhere.com";

        var act = () => _testTool.Execute(path,
            [
                new Flag("name", name),
                new Flag("options", options),
                new Flag("unknown", "")
            ]);

        act.Should().Throw<FlagException>();
    }

    [Fact]
    public void Add_ParseFlags_UnknownShellFlag()
    {
        var name = "testName";
        var options = "testOptions";
        var path = Path.Combine(_config.TestFilesPath, _config.BashScriptName);

        var act = () => _testTool.Execute(path,
            [
                new Flag("name", name),
                new Flag("options", options),
                new Flag("script", "unknown")
            ]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_ParseFlags_ThrowIfCMDOnLinux()
    {
        var name = "testName";
        var options = "testOptions";
        var path = Path.Combine(_config.TestFilesPath, _config.BashScriptName);

        var act = () => _testTool.Execute(path,
            [
                new Flag("name", name),
                new Flag("options", options),
                new Flag("script", "cmd")
            ]);

        if (Environment.OSVersion.Platform == PlatformID.Unix)
            act.Should().Throw<ArgumentException>();
        else
        {
            act();

            _factory.Verify(factory =>
                factory.Construct(It.Is<CommandAlterRequest>(req =>
                    req.Name == name
                    && req.Path == path
                    && req.Type == CommandType.Script
                    && req.Shell == ShellType.Cmd
                    && req.Options == options)),
                Times.Once);
        }
    }

    [Theory]
    [InlineData("exe")]
    [InlineData("url")]
    public void Add_ParseFlags_TypeFlagsWithNoValue(string flag)
    {
        var name = "testName";
        var options = "testOptions";
        var path = Path.Combine(_config.TestFilesPath, _config.CmdScriptName);

        var act = () => _testTool.Execute(path,
            [
                new Flag("name", name),
                new Flag("options", options),
                new Flag(flag, "value")
            ]);

        act.Should().Throw<FlagException>();
    }

    [Theory]
    [InlineData("https://some.com/", "some")]
    [InlineData("http://any.com/", "any")]
    [InlineData("https://somewhere.com/some/", "somewhere")]
    [InlineData("http://anyone.com/some/", "anyone")]
    public void Add_ParseUrl(string url, string name)
    {
        _testTool.Execute(url, []);

        _factory.Verify(factory =>
            factory.Construct(It.Is<CommandAlterRequest>(req =>
                req.Name == name
                && req.Path == url
                && req.Type == CommandType.Website)),
            Times.Once);
    }

    [Theory]
    [InlineData("cmd", CommandType.Script, ShellType.Cmd)]
    [InlineData("ps", CommandType.Script, ShellType.Powershell)]
    [InlineData("bash", CommandType.Script, ShellType.Bash)]
    [InlineData("exe", CommandType.Executable, ShellType.None)]
    [InlineData("txt", CommandType.None, ShellType.None)]
    public void Add_ParsePaths(string file, CommandType type, ShellType shell)
    {
        var fileName = file switch
        {
            "cmd" => _config.CmdScriptName,
            "ps" => _config.PSScriptName,
            "bash" => _config.BashScriptName,
            "exe" => _config.ExecutableName,
            "txt" => _config.TxtName,
            _ => throw new CriticalException()
        };
        var path = Path.Combine(_config.TestFilesPath, fileName);

        var act = () => _testTool.Execute(path, []);

        if (fileName != _config.TxtName)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix
                && type == CommandType.Script
                && shell == ShellType.Cmd)
            {
                act.Should().Throw<ArgumentException>();
                return;
            }
            else act.Should().NotThrow();

            var name = fileName.Split('.')[0];
            _factory.Verify(factory =>
                factory.Construct(It.Is<CommandAlterRequest>(req =>
                    req.Name == name
                    && req.Path == path
                    && req.Type == type
                    && req.Shell == shell)),
                Times.Once);
        }
        else act.Should().Throw<ArgumentException>();

    }

    [Fact]
    public void Add_ShouldTrow_IfUnknownArg()
    {
        var act = () => _testTool.Execute("unknown", []);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("add")]
    [InlineData("ls")]
    [InlineData("rm")]
    [InlineData("test1")]
    [InlineData("test2")]
    [InlineData("test3")]
    public void Add_ShouldThrow_IfNameExists(string name)
    {
        var path = Path.Combine(_config.TestFilesPath, _config.CmdScriptName);

        var act = () => _testTool.Execute(path, [new Flag("name", name)]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_ShouldThrow_IfCMDOnLinux()
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            var path = Path.Combine(_config.TestFilesPath, _config.ExecutableName);

            var act = () => _testTool.Execute(path, [new Flag("script", "cmd"),]);

            act.Should().Throw<ArgumentException>();
        }
    }

    [Fact]
    public void Add_ShouldSaveCommand()
    {
        _testTool.Execute(_factory.CommandSavable.Path,
            [
                new Flag("name", _factory.CommandSavable.Name),
                new Flag("options", _factory.CommandSavable.Options),
                new Flag("script", "bash")
            ]);

        _factory.Verify(factory =>
            factory.Construct(It.Is<CommandAlterRequest>(req =>
                req.Name == _factory.CommandSavable.Name
                && req.Path == _factory.CommandSavable.Path
                && req.Type == CommandType.Script
                && req.Shell == ShellType.Bash
                && req.Options == _factory.CommandSavable.Options)),
            Times.Once);
        _loader.Verify(loader =>
            loader.SaveCommand(_factory.CommandSavable), Times.Once);
    }
}