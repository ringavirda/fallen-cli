using Moq;

using FCli.Exceptions;
using FCli.Models.Tools;
using static FCli.Models.Args;
using FCli.Services;
using FCli.Services.Data;
using FCli.Services.Format;
using FCli.Models.Types;
using FCli.Services.Config;
using System.Resources;

namespace FCli.Tests.Models.Tools;

public class AddTests
{
    private static readonly AddTool _testTool;

    private static readonly Mock<IToolExecutor> _fakeExecutor;
    private static readonly Mock<ICommandFactory> _fakeFactory;
    private static readonly Mock<ICommandLoader> _fakeLoader;
    private static readonly Mock<ICommandLineFormatter> _fakeFormatter;
    private static readonly Mock<IConfig> _fakeConfig;
    private static readonly Mock<ResourceManager> _fakeResources;

    static AddTests()
    {
        _fakeExecutor = TestRepository.ToolExecutorFake;
        _fakeFactory = TestRepository.CommandFactoryFake;
        _fakeLoader = TestRepository.CommandLoaderFake;
        _fakeFormatter = TestRepository.FormatterFake;
        _fakeConfig = TestRepository.ConfigFake;
        _fakeResources = TestRepository.ResourcesFake;

        _testTool = new AddTool(
            _fakeFormatter.Object,
            _fakeResources.Object,
            _fakeExecutor.Object,
            _fakeFactory.Object,
            _fakeLoader.Object,
            _fakeConfig.Object);
    }

    [Fact]
    public void Add_HandleHelp()
    {
        var act = () => _testTool.Action("", new() { new Flag("help", "") });

        act.Should().NotThrow();
        _fakeFormatter.Verify(formatter =>
            formatter.DisplayMessage(_testTool.Description), Times.Once);
    }

    [Fact]
    public void Add_ShouldHaveArg()
    {
        var act = () => _testTool.Action("", new() { new Flag("flag", "") });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_ShouldHaveOnlyOneTypeFlag()
    {
        var act = () => _testTool.Action("test", new() { new Flag("exe", ""), new Flag("url", "") });

        act.Should().Throw<FlagException>();
    }

    [Fact]
    public void Add_ParseFlags_Name()
    {
        var name = "testName";
        var path = Path.Combine(TestRepository.TestFilesPath, TestRepository.BashScriptName);

        _testTool.Action(path, new List<Flag>() { new Flag("name", name) });

        _fakeFactory.Verify(factory => factory.Construct(
            name,
            path,
            CommandType.Script,
            ShellType.Bash,
            ""), Times.Once);
    }

    [Fact]
    public void Add_ParseFlags_Options()
    {
        var options = "testOptions";
        var path = Path.Combine(TestRepository.TestFilesPath, TestRepository.BashScriptName);

        _testTool.Action(path,
        new List<Flag>() { new Flag("options", options) });

        var commandName = TestRepository.BashScriptName.Split('.')[0];
        _fakeFactory.Verify(factory => factory.Construct(
            commandName,
            path,
            CommandType.Script,
            ShellType.Bash,
            options), Times.Once);
    }

    [Fact]
    public void Add_ParseFlags_NameAndOptions()
    {
        var name = "testName";
        var options = "testOptions";
        var path = Path.Combine(TestRepository.TestFilesPath, TestRepository.BashScriptName);

        _testTool.Action(path,
            new List<Flag>()
            {
                new Flag("name", name),
                new Flag("options", options)
            });

        _fakeFactory.Verify(factory => factory.Construct(
            name,
            path,
            CommandType.Script,
            ShellType.Bash,
            options), Times.Once);
    }

    [Theory]
    [InlineData("exe", "", CommandType.Executable, ShellType.None)]
    [InlineData("url", "", CommandType.Website, ShellType.None)]
    [InlineData("script", "bash", CommandType.Script, ShellType.Bash)]
    [InlineData("script", "powershell", CommandType.Script, ShellType.Powershell)]
    [InlineData("script", "cmd", CommandType.Script, ShellType.Cmd)]
    public void Add_ParseFlags_TypeFlags(string flag, string value, CommandType commandType, ShellType shellType)
    {
        var name = "testName1";
        var options = "testOptions1";
        string path;
        if (flag == "url")
            path = "https://somwhere.com";
        else
            path = Path.Combine(TestRepository.TestFilesPath, TestRepository.PSScriptName);

        var act = () => _testTool.Action(path,
            new List<Flag>()
            {
                new Flag("name", name),
                new Flag("options", options),
                new Flag(flag, value)
            });

        if (Environment.OSVersion.Platform == PlatformID.Unix
            && commandType == CommandType.Script
            && shellType == ShellType.Cmd)
        {
            act.Should().Throw<ArgumentException>();
            return;
        }

        act.Should().NotThrow();
        _fakeFactory.Verify(factory => factory.Construct(
        name,
        path,
        commandType,
        shellType,
        options), Times.Once);
    }

    [Fact]
    public void Add_ParseFlags_TypeFlagsShouldThrowIfUnknown()
    {
        var name = "testName";
        var options = "testOptions";
        var path = "https://somwhere.com";

        var act = () => _testTool.Action(path,
            new List<Flag>
            {
                new Flag("name", name),
                new Flag("options", options),
                new Flag("unknown", "")
            });

        act.Should().Throw<FlagException>();
    }

    [Fact]
    public void Add_ParseFlags_UnknownShellFlag()
    {
        var name = "testName";
        var options = "testOptions";
        var path = Path.Combine(TestRepository.TestFilesPath, TestRepository.BashScriptName);

        var act = () => _testTool.Action(path,
            new List<Flag>()
            {
                new Flag("name", name),
                new Flag("options", options),
                new Flag("script", "unknown")
            });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_ParseFlags_ThrowIfCMDOnLinux()
    {
        var name = "testName";
        var options = "testOptions";
        var path = Path.Combine(TestRepository.TestFilesPath, TestRepository.BashScriptName);

        var act = () => _testTool.Action(path,
            new List<Flag>()
            {
                new Flag("name", name),
                new Flag("options", options),
                new Flag("script", "cmd")
            });

        if (Environment.OSVersion.Platform == PlatformID.Unix)
            act.Should().Throw<ArgumentException>();
        else
        {
            act();
            _fakeFactory.Verify(factory => factory.Construct(
                name,
                Path.Combine(TestRepository.TestFilesPath, TestRepository.BashScriptName),
                CommandType.Script,
                ShellType.Cmd,
                options), Times.Once);
        }
    }

    [Theory]
    [InlineData("exe")]
    [InlineData("url")]
    public void Add_ParseFlags_TypeFlagsWithNoValue(string flag)
    {
        var name = "testName";
        var options = "testOptions";
        var path = Path.Combine(TestRepository.TestFilesPath, TestRepository.CmdScriptName);

        var act = () => _testTool.Action(path,
            new List<Flag>()
            {
                new Flag("name", name),
                new Flag("options", options),
                new Flag(flag, "value")
            });

        act.Should().Throw<FlagException>();
    }

    [Theory]
    [InlineData("https://some.com/", "some")]
    [InlineData("http://any.com/", "any")]
    [InlineData("https://somewhere.com/some/", "somewhere")]
    [InlineData("http://anyone.com/some/", "anyone")]
    public void Add_ParseUrl(string url, string name)
    {
        _fakeFactory.Invocations.Clear();
        _testTool.Action(url, new());

        _fakeFactory.Verify(factory => factory.Construct(
                name,
                url,
                CommandType.Website,
                ShellType.None,
                ""), Times.Once);
    }

    [Fact]
    public void Add_ParsePaths()
    {
        var testFiles = new Dictionary<string, (CommandType, ShellType)> {
            {TestRepository.CmdScriptName, (CommandType.Script, ShellType.Cmd)},
            {TestRepository.PSScriptName, (CommandType.Script, ShellType.Powershell)},
            {TestRepository.BashScriptName, (CommandType.Script, ShellType.Bash)},
            {TestRepository.ExecutableName, (CommandType.Executable, ShellType.None)},
            {TestRepository.TxtName, (CommandType.None, ShellType.None)}
        };

        foreach (var file in testFiles.Keys)
        {
            var path = Path.Combine(TestRepository.TestFilesPath, file);

            var act = () => _testTool.Action(path, new());

            if (file != TestRepository.TxtName)
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix
                    && testFiles[file].Item1 == CommandType.Script
                    && testFiles[file].Item2 == ShellType.Cmd)
                {
                    act.Should().Throw<ArgumentException>();
                    return;
                }
                else act.Should().NotThrow();

                var name = file.Split('.')[0];
                _fakeFactory.Verify(factory => factory.Construct(
                        name,
                        path,
                        testFiles[file].Item1,
                        testFiles[file].Item2,
                        ""), Times.Once);
            }
            else act.Should().Throw<ArgumentException>();
        }
    }

    [Fact]
    public void Add_ShouldTrow_IfUnknownArg()
    {
        var act = () => _testTool.Action("unknown", new List<Flag>());

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
        var path = Path.Combine(TestRepository.TestFilesPath, TestRepository.CmdScriptName);

        var act = () => _testTool.Action(path,
            new List<Flag>()
            {
                new Flag("name", name),
            });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_ShouldThrow_IfCMDOnLinux()
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            var path = Path.Combine(TestRepository.TestFilesPath, TestRepository.ExecutableName);

            var act = () => _testTool.Action(path,
                new List<Flag>()
                {
                    new Flag("script", "cmd"),
                });

            act.Should().Throw<ArgumentException>();
        }
    }

    [Fact]
    public void Add_ShouldSaveCommand()
    {
        _fakeFactory.Invocations.Clear();

        _testTool.Action(TestRepository.CommandSavable.Path, new List<Flag>()
        {
            new Flag("name", TestRepository.CommandSavable.Name),
            new Flag("options", TestRepository.CommandSavable.Options),
            new Flag("script", "bash")
        });

        _fakeFactory.Verify(factory => factory.Construct(
            TestRepository.CommandSavable.Name,
            TestRepository.CommandSavable.Path,
            CommandType.Script,
            ShellType.Bash,
            TestRepository.CommandSavable.Options), Times.Once);
        _fakeLoader.Verify(loader =>
            loader.SaveCommand(TestRepository.CommandSavable), Times.Once);
    }
}