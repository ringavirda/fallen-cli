using FCli.Services;
using FCli.Services.Data;
using FCli.Models;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FCli.Tests.Services.Storage;

public class JsonLoaderTests : IDisposable
{
    private readonly Mock<DynamicConfig> _dynamicFake;
    private readonly Mock<IConfiguration> _configurationFake;

    private readonly JsonLoader _loader;
    private readonly string _fullSavePath = "Test/TestStorage/storage.json";

    public JsonLoaderTests()
    {
        _dynamicFake = new Mock<DynamicConfig>();
        _dynamicFake.SetupGet(dyn => dyn.AppFolderPath).Returns("Test");
        _dynamicFake.SetupGet(dyn => dyn.StorageFileName).Returns("TestStorage");

        _configurationFake = new Mock<IConfiguration>();
        _configurationFake.Setup(conf => conf.GetSection("Storage").GetSection("StorageFolderName").Value).Returns("TestStorage");

        _loader = new JsonLoader(_dynamicFake.Object, _configurationFake.Object);
    }

    [Fact]
    public void JsonLoader_Create()
    {
        var loader = new JsonLoader(_dynamicFake.Object, _configurationFake.Object);

        _dynamicFake.Verify(dyn => dyn.AppFolderPath, Times.Exactly(2));
        _configurationFake.Verify(conf => conf.GetSection("Storage").GetSection("StorageFolderName").Value, Times.Exactly(2));

        Directory.Exists("Test").Should().BeTrue();
        Directory.Exists(Path.Combine("Test", "TestStorage")).Should().BeTrue();
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

        File.Exists(_fullSavePath).Should().BeTrue();
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
        var exists = _loader.CommandExists(command.Name);

        exists.Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_CommandExists_Fails()
    {
        if (File.Exists(_fullSavePath))
            File.Delete(_fullSavePath);

        var exists = _loader.CommandExists("test");

        exists.Should().BeFalse();
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
        if (File.Exists(_fullSavePath))
            File.Delete(_fullSavePath);

        var commands = _loader.LoadCommands();

        commands.Should().BeEmpty();
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
        var exists = _loader.CommandExists(command.Name);

        exists.Should().BeFalse();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists("Test"))
            Directory.Delete("Test", true);
    }
}