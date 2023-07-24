using Moq;

using FCli.Services;
using FCli.Services.Data;
using FCli.Models;

namespace FCli.Tests.Services.Storage;

public class JsonLoaderTests : IAsyncDisposable
{
    private readonly Mock<IConfig> _dynamicFake;

    private readonly JsonLoader _loader;
    private readonly static string _testFolderName = "FCliTest";
    private readonly static string _testStorageFileName 
        = $"{Guid.NewGuid()}.json";
    private readonly static string _testStoragePath 
        = Path.Combine(_testFolderName, _testStorageFileName) ;

    public JsonLoaderTests()
    {
        _dynamicFake = new Mock<IConfig>();
        _dynamicFake.SetupGet(dyn => dyn.AppFolderName)
            .Returns(_testFolderName);
        _dynamicFake.SetupGet(dyn => dyn.AppFolderPath)
            .Returns(_testFolderName);
        _dynamicFake.SetupGet(dyn => dyn.StorageFileName)
            .Returns(_testStorageFileName);
        _dynamicFake.SetupGet(dyn => dyn.StorageFilePath)
            .Returns(Path.Combine(_testFolderName, _testStorageFileName));

        _loader = new JsonLoader(_dynamicFake.Object);
    }

    [Fact]
    public void JsonLoader_Create()
    {
        var loader = new JsonLoader(_dynamicFake.Object);

        _dynamicFake.Verify(dyn => dyn.AppFolderPath, Times.Exactly(2));

        Directory.Exists(_testFolderName).Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_SaveCommand()
    {
        var command = new Command()
        {
            Name = "test",
            Path = "test/path",
            Type = CommandType.Bash,
            Options = "test_options",
            Action = () => { }
        };
        _loader.SaveCommand(command);

        File.Exists(_testStoragePath).Should().BeTrue();
        _loader.CommandExists(command.Name).Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_CommandExists_Correctly()
    {
        var command = new Command()
        {
            Name = "test",
            Path = "test/path",
            Type = CommandType.Bash,
            Options = "test_options",
            Action = () => { }
        };

        _loader.SaveCommand(command);

        _loader.CommandExists(command.Name).Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_CommandExists_Fails()
    {
        if (File.Exists(_testStoragePath))
            File.Delete(_testStoragePath);

        _loader.CommandExists("test").Should().BeFalse();
    }

    [Fact]
    public void JsonLoader_LoadCommands()
    {
        var command1 = new Command()
        {
            Name = "test1",
            Path = "test/path",
            Type = CommandType.Bash,
            Options = "test_options",
            Action = () => { }
        };
        var command2 = new Command()
        {
            Name = "test2",
            Path = "test/path",
            Type = CommandType.Bash,
            Options = "test_options",
            Action = () => { }
        };

        _loader.SaveCommand(command1);
        _loader.SaveCommand(command2);
        var commands = _loader.LoadCommands();

        commands.Should().Contain(c => c.Name == command1.Name
            && c.Options == command1.Options
            && c.Path == command1.Path);
        commands.Should().Contain(c => c.Name == command2.Name
            && c.Options == command2.Options
            && c.Path == command2.Path);
    }

    [Fact]
    public void JsonLoader_LoadCommands_NoCommands()
    {
        if (File.Exists(_testStoragePath))
            File.Delete(_testStoragePath);

        _loader.LoadCommands().Should().BeNull();
    }

    [Fact]
    public void JsonLoader_DeleteCommand()
    {
        var command = new Command()
        {
            Name = "test",
            Path = "test/path",
            Type = CommandType.Bash,
            Options = "test_options",
            Action = () => { }
        };

        _loader.SaveCommand(command);
        _loader.DeleteCommand(command.Name);

        _loader.CommandExists(command.Name).Should().BeFalse();
    }
    
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_testFolderName))
            Directory.Delete(_testFolderName, true);
        await Task.CompletedTask;
    }
}