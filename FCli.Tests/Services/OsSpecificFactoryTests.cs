using FCli.Common.Exceptions;
using FCli.Services;
using FCli.Services.Data;
using Moq;

namespace FCli.Tests.Services;

public class OSSpecificFactoryTests
{
    private static readonly OSSpecificFactory _testFactory;

    private static readonly Mock<ICommandLoader> _fakeLoader;

    static OSSpecificFactoryTests()
    {
        _fakeLoader = TestRepository.CommandLoaderFake;

        _testFactory = new OSSpecificFactory(_fakeLoader.Object);
    }

    [Fact]
    public void OSSpecificFactory_ConstructFromTemplate_Executable()
    {
        var command = _testFactory.Construct(
            TestRepository.TestCommand.Name,
            "dotnet",
            FCli.Models.CommandType.Executable,
            "--help");
        
        command.Should().NotBeNull();
        command.Name.Should().Be(TestRepository.TestCommand.Name);
        command.Path.Should().Be("dotnet");
        command.Type.Should().Be(FCli.Models.CommandType.Executable);
        command.Options.Should().Be("--help");

        command.Action.Should().NotThrow();
    }
    
    [Fact]
    public void OSSpecificFactory_ConstructFromTemplate_Website()
    {
        var command = _testFactory.Construct(
            TestRepository.TestCommand.Name,
            "https://google.com",
            FCli.Models.CommandType.Url,
            "");
        
        command.Should().NotBeNull();
        command.Name.Should().Be(TestRepository.TestCommand.Name);
        command.Path.Should().Be("https://google.com");
        command.Type.Should().Be(FCli.Models.CommandType.Url);
        command.Options.Should().Be("");

        command.Action.Should().NotThrow();
    }

    [Fact]
    public void OSSpecificFactory_ConstructFromTemplate_CMD()
    {
        var scriptPath = Path.Combine(
            TestRepository.TestFilesPath,
            TestRepository.TestCmdScriptName);
            
        var command = _testFactory.Construct(
            TestRepository.TestCommand.Name,
            scriptPath,
            FCli.Models.CommandType.CMD,
            "");
        
        command.Should().NotBeNull();
        command.Name.Should().Be(TestRepository.TestCommand.Name);
        command.Path.Should().Be(scriptPath);
        command.Type.Should().Be(FCli.Models.CommandType.CMD);
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
            TestRepository.TestPSScriptName);
            
        var command = _testFactory.Construct(
            TestRepository.TestCommand.Name,
            scriptPath,
            FCli.Models.CommandType.Powershell,
            "");
        
        command.Should().NotBeNull();
        command.Name.Should().Be(TestRepository.TestCommand.Name);
        command.Path.Should().Be(scriptPath);
        command.Type.Should().Be(FCli.Models.CommandType.Powershell);
        command.Options.Should().Be("");
        
        command.Action.Should().NotThrow();
    }

    [Fact]
    public void OSSpecificFactory_ConstructFromTemplate_Bash()
    {
        var scriptPath = Path.Combine(
            TestRepository.TestFilesPath,
            TestRepository.TestBashScriptName);
            
        var command = _testFactory.Construct(
            TestRepository.TestCommand.Name,
            scriptPath,
            FCli.Models.CommandType.Bash,
            "");
        
        command.Should().NotBeNull();
        command.Name.Should().Be(TestRepository.TestCommand.Name);
        command.Path.Should().Be(scriptPath);
        command.Type.Should().Be(FCli.Models.CommandType.Bash);
        command.Options.Should().Be("");

        command.Action.Should().NotThrow();
    }

    [Fact]
    public void OSSpecificFactory_ConstructFromTemplate_CriticalFail()
    {
        var act = () => _testFactory.Construct(
            TestRepository.TestCommand.Name,
            "none",
            FCli.Models.CommandType.None,
            "");
        
        act.Should().Throw<CriticalException>();
    }

    [Fact]
    public void OSSpecificFactory_ConstructFromLoader()
    {
        _fakeLoader.Invocations.Clear();
        var command = _testFactory.Construct(TestRepository.TestCommand.Name);
        
        _fakeLoader.Verify(loader => loader.LoadCommand(TestRepository.TestCommand.Name), Times.Once);

        command.Name.Should().Be(TestRepository.TestCommand.Name);
        command.Path.Should().Be(TestRepository.TestCommand.Path);
        command.Type.Should().Be(TestRepository.TestCommand.Type);
        command.Options.Should().Be(TestRepository.TestCommand.Options);
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