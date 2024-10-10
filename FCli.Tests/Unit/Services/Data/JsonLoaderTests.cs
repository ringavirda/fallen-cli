using FCli.Exceptions;
using FCli.Services.Data;
using FCli.Tests.Fixtures;

namespace FCli.Tests.Unit.Services.Data;

[Collection("Common")]
public class JsonLoaderTests : IClassFixture<FactoryFixture>, IDisposable
{
    private readonly JsonLoader _testLoader;
    private readonly ConfigFixture _config;
    private readonly FactoryFixture _factory;

    public JsonLoaderTests(FactoryFixture factory)
    {
        var config = new ConfigFixture();
        _testLoader = new JsonLoader(config.Object);
        _config = config;
        _factory = factory;
    }

    [Fact]
    public void JsonLoader_Create()
    {
        _ = new JsonLoader(_config.Object);

        Directory.Exists(_config.FolderName).Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_SaveCommand()
    {
        _testLoader.SaveCommand(_factory.Command1);

        File.Exists(_config.StoragePath).Should().BeTrue();
        _testLoader.CommandExists(_factory.Command1.Name)
            .Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_CommandExists_Correctly()
    {
        _testLoader.SaveCommand(_factory.Command1);

        _testLoader.CommandExists(_factory.Command1.Name).Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_CommandExists_Fails()
    {
        if (File.Exists(_config.StoragePath))
            File.Delete(_config.StoragePath);

        _testLoader.CommandExists("test").Should().BeFalse();
    }

    [Fact]
    public void JsonLoader_LoadCommand_Buffer()
    {
        _testLoader.SaveCommand(_factory.Command1);
        var command1 = _testLoader.LoadCommand(_factory.Command1.Name);
        var command2 = _testLoader.LoadCommand(_factory.Command1.Name);

        ReferenceEquals(command1, command2).Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_LoadCommands()
    {
        _testLoader.SaveCommand(_factory.Command1);
        _testLoader.SaveCommand(_factory.Command2);
        _testLoader.SaveCommand(_factory.Command3);

        var commands = _testLoader.LoadCommands();

        commands.Should().HaveCount(3)
            .And.Contain(command => command.Name == _factory.Command1.Name)
            .And.Contain(command => command.Name == _factory.Command2.Name)
            .And.Contain(command => command.Name == _factory.Command3.Name);
    }

    [Fact]
    public void JsonLoader_LoadCommands_NoCommands()
    {
        if (File.Exists(_config.StoragePath))
            File.Delete(_config.StoragePath);

        _testLoader.LoadCommands().Should().BeNull();
    }

    [Fact]
    public void JsonLoader_DeleteCommand()
    {
        _testLoader.SaveCommand(_factory.Command1);
        _testLoader.DeleteCommand(_factory.Command1.Name);

        _testLoader.CommandExists(_factory.Command1.Name)
            .Should().BeFalse();
    }

    [Fact]
    public void JsonLoader_DeleteCommand_UnknownCommand()
    {
        var act = () => _testLoader.DeleteCommand("unknown");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JsonLoader_CriticalException_IfDeserializationFails()
    {
        if (!Directory.Exists(_config.FolderName))
            Directory.CreateDirectory(_config.FolderName);
        File.WriteAllText(_config.StoragePath, "{}");

        var act = _testLoader.LoadCommands;

        act.Should().Throw<CriticalException>();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_config.FolderName))
            Directory.Delete(_config.FolderName, true);
    }
}