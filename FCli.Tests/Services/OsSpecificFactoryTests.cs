using Moq;

using FCli.Exceptions;
using FCli.Services;
using FCli.Services.Data;
using FCli.Services.Format;
using FCli.Models.Types;
using System.Resources;

namespace FCli.Tests.Services;

public class OSSpecificFactoryTests
{
    private static readonly SystemSpecificFactory _testFactory;

    private static readonly Mock<ICommandLoader> _fakeLoader;
    private static readonly Mock<ICommandLineFormatter> _fakeFormatter;
    private static readonly Mock<ResourceManager> _fakeResources;

    static OSSpecificFactoryTests()
    {
        _fakeLoader = TestRepository.CommandLoaderFake;
        _fakeFormatter = TestRepository.FormatterFake;
        _fakeResources = TestRepository.ResourcesFake;

        _testFactory = new SystemSpecificFactory(
            _fakeLoader.Object,
            _fakeFormatter.Object,
            _fakeResources.Object);
    }

    [Fact]
    public void OSSpecificFactory_ConstructFromTemplate_Executable()
    {
        var command = _testFactory.Construct(
            "test",
            "dotnet",
            CommandType.Executable,
            ShellType.None,
            "--help");
        
        command.Should().NotBeNull();
        command.Name.Should().Be("test");
        command.Path.Should().Be("dotnet");
        command.Type.Should().Be(CommandType.Executable);
        command.Shell.Should().Be(ShellType.None);
        command.Options.Should().Be("--help");

        command.Action.Should().NotThrow();
    }
    
    [Fact]
    public void OSSpecificFactory_ConstructFromTemplate_Website()
    {
        var command = _testFactory.Construct(
            "test",
            "https://google.com",
            CommandType.Website,
            ShellType.None,
            "");
        
        command.Should().NotBeNull();
        command.Name.Should().Be("test");
        command.Path.Should().Be("https://google.com");
        command.Type.Should().Be(CommandType.Website);
        command.Shell.Should().Be(ShellType.None);
        command.Options.Should().Be("");

        command.Action.Should().NotThrow();
    }

    [Fact]
    public void OSSpecificFactory_ConstructFromTemplate_CMD()
    {
        var scriptPath = Path.Combine(
            TestRepository.TestFilesPath,
            TestRepository.CmdScriptName);
            
        var command = _testFactory.Construct(
            "test",
            scriptPath,
            CommandType.Script,
            ShellType.Cmd,
            "");
        
        command.Should().NotBeNull();
        command.Name.Should().Be("test");
        command.Path.Should().Be(scriptPath);
        command.Type.Should().Be(CommandType.Script);
        command.Shell.Should().Be(ShellType.Cmd);
        command.Options.Should().Be("");
        
        if (Environment.OSVersion.Platform == PlatformID.Unix)
            command.Action.Should().Throw<InvalidOperationException >();
        else command.Action.Should().NotThrow();
    }

    [Fact]
    public void OSSpecificFactory_ConstructFromTemplate_Powershell()
    {
        var scriptPath = Path.Combine(
            TestRepository.TestFilesPath,
            TestRepository.PSScriptName);
            
        var command = _testFactory.Construct(
            "test",
            scriptPath,
            CommandType.Script,
            ShellType.Powershell,
            "");
        
        command.Should().NotBeNull();
        command.Name.Should().Be("test");
        command.Path.Should().Be(scriptPath);
        command.Type.Should().Be(CommandType.Script);
        command.Shell.Should().Be(ShellType.Powershell);
        command.Options.Should().Be("");
        
        command.Action.Should().NotThrow();
    }

    [Fact]
    public void OSSpecificFactory_ConstructFromTemplate_Bash()
    {
        var scriptPath = Path.Combine(
            TestRepository.TestFilesPath,
            TestRepository.BashScriptName);
            
        var command = _testFactory.Construct(
            "test",
            scriptPath,
            CommandType.Script,
            ShellType.Bash,
            "");
        
        command.Should().NotBeNull();
        command.Name.Should().Be("test");
        command.Path.Should().Be(scriptPath);
        command.Type.Should().Be(CommandType.Script);
        command.Shell.Should().Be(ShellType.Bash);
        command.Options.Should().Be("");

        command.Action.Should().NotThrow();
    }

    [Fact]
    public void OSSpecificFactory_ConstructFromTemplate_CriticalFail()
    {
        var act = () => _testFactory.Construct(
            "test",
            "none",
            CommandType.None,
            ShellType.None,
            "");
        
        act.Should().Throw<CriticalException>();
    }

    [Fact]
    public void OSSpecificFactory_ConstructFromLoader()
    {
        _fakeLoader.Invocations.Clear();

        var command = _testFactory.Construct(TestRepository.Command1.Name);
        
        command.Name.Should().Be(TestRepository.Command1.Name);
        command.Path.Should().Be(TestRepository.Command1.Path);
        command.Type.Should().Be(TestRepository.Command1.Type);
        command.Options.Should().Be(TestRepository.Command1.Options);
        _fakeLoader.Verify(loader => 
            loader.LoadCommand(TestRepository.Command1.Name), 
            Times.Once);
    }

    [Fact]
    public void OSSpecificFactory_ConstructFromLoader_Fails()
    {
        _fakeLoader.Invocations.Clear();

        var act = () => _testFactory.Construct("none");
        
        act.Should().Throw<InvalidOperationException>();
        _fakeLoader.Verify(loader => loader.LoadCommand("none"), Times.Once);
    }
}