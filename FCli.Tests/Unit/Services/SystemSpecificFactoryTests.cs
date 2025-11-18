using FCli.Exceptions;
using FCli.Models.Dtos;
using FCli.Models.Types;
using FCli.Services;
using FCli.Tests.Fixtures;

using Moq;

namespace FCli.Tests.Unit.Services;

[Collection("Common")]
public class SystemSpecificFactoryTests
    : IClassFixture<LoaderFixture>, IClassFixture<FactoryFixture>
{
    private readonly SystemSpecificFactory _testFactory;
    private readonly ConfigFixture _config;
    private readonly LoaderFixture _loader;
    private readonly FactoryFixture _factory;

    public SystemSpecificFactoryTests(
        ConfigFixture config,
        LoaderFixture loader,
        FormatterFixture formatter,
        ResourcesFixture resources,
        FactoryFixture factory)
    {
        _testFactory = new SystemSpecificFactory(
            loader.Object,
            formatter.Object,
            resources.Object);
        _config = config;
        _loader = loader;
        _factory = factory;

        _loader.Invocations.Clear();
    }

    [Fact]
    public void SystemSpecificFactory_ConstructFromTemplate_Executable()
    {
        var request = new CommandAlterRequest
        {
            Name = "test",
            Path = "dotnet",
            Type = CommandType.Executable,
            Shell = ShellType.None,
            Options = "--help"
        };

        var command = _testFactory.Construct(request);

        command.Should().NotBeNull();
        command.Action.Should().NotBeNull();

        command.Name.Should().Be("test");
        command.Path.Should().Be("dotnet");
        command.Type.Should().Be(CommandType.Executable);
        command.Shell.Should().Be(ShellType.None);
        command.Options.Should().Be("--help");
    }

    [Fact]
    public void SystemSpecificFactory_ConstructFromTemplate_Website()
    {
        var request = new CommandAlterRequest
        {
            Name = "test",
            Path = "https://google.com",
            Type = CommandType.Website,
            Shell = ShellType.None,
        };

        var command = _testFactory.Construct(request);

        command.Should().NotBeNull();
        command.Action.Should().NotBeNull();

        command.Name.Should().Be("test");
        command.Path.Should().Be("https://google.com");
        command.Type.Should().Be(CommandType.Website);
        command.Shell.Should().Be(ShellType.None);
        command.Options.Should().Be("");
    }

    [Fact]
    public void SystemSpecificFactory_ConstructFromTemplate_CMD()
    {
        var scriptPath = Path.Combine(
            _config.TestFilesPath,
            _config.CmdScriptName);
        var request = new CommandAlterRequest
        {
            Name = "test",
            Path = scriptPath,
            Type = CommandType.Script,
            Shell = ShellType.Cmd,
        };

        var command = _testFactory.Construct(request);

        command.Should().NotBeNull();
        command.Action.Should().NotBeNull();

        command.Name.Should().Be("test");
        command.Path.Should().Be(scriptPath);
        command.Type.Should().Be(CommandType.Script);
        command.Shell.Should().Be(ShellType.Cmd);
        command.Options.Should().Be("");
    }

    [Fact]
    public void SystemSpecificFactory_ConstructFromTemplate_Powershell()
    {
        var scriptPath = Path.Combine(
            _config.TestFilesPath,
            _config.PSScriptName);
        var request = new CommandAlterRequest
        {
            Name = "test",
            Path = scriptPath,
            Type = CommandType.Script,
            Shell = ShellType.Powershell,
        };

        var command = _testFactory.Construct(request);

        command.Should().NotBeNull();
        command.Action.Should().NotBeNull();

        command.Name.Should().Be("test");
        command.Path.Should().Be(scriptPath);
        command.Type.Should().Be(CommandType.Script);
        command.Shell.Should().Be(ShellType.Powershell);
        command.Options.Should().Be("");
    }

    [Fact]
    public void SystemSpecificFactory_ConstructFromTemplate_CriticalFail()
    {
        var request = new CommandAlterRequest
        {
            Name = "test",
            Path = "none",
            Type = CommandType.None,
            Shell = ShellType.None,
        };

        var act = () => _testFactory.Construct(request);

        act.Should().Throw<CriticalException>();
    }

    [Fact]
    public void SystemSpecificFactory_ConstructFromLoader()
    {
        var command = _testFactory.Construct(_factory.Command1.Name);

        command.Name.Should().Be(_factory.Command1.Name);
        command.Path.Should().Be(_factory.Command1.Path);
        command.Type.Should().Be(_factory.Command1.Type);
        command.Options.Should().Be(_factory.Command1.Options);
        _loader.Verify(loader =>
            loader.LoadCommand(_factory.Command1.Name),
            Times.Once);
    }

    [Fact]
    public void SystemSpecificFactory_ConstructFromLoader_Fails()
    {
        var act = () => _testFactory.Construct("none");

        act.Should().Throw<InvalidOperationException>();
        _loader.Verify(loader => loader.LoadCommand("none"), Times.Once);
    }
}