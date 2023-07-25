using FCli.Services.Data;
using FCli.Exceptions;

namespace FCli.Tests.Services.Storage;

public class JsonLoaderTests : IDisposable
{
    private static JsonLoader TestLoader 
        => new (TestRepository.ConfigFake.Object);

    [Fact]
    public void JsonLoader_Create()
    {
        _ = new JsonLoader(TestRepository.ConfigFake.Object);

        Directory.Exists(TestRepository.FolderName).Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_SaveCommand()
    {
        TestLoader.SaveCommand(TestRepository.Command1);

        File.Exists(TestRepository.StoragePath).Should().BeTrue();
        TestLoader.CommandExists(TestRepository.Command1.Name)
            .Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_CommandExists_Correctly()
    {
        var loader = TestLoader;
        loader.SaveCommand(TestRepository.Command1);

        loader.CommandExists(TestRepository.Command1.Name).Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_CommandExists_Fails()
    {
        if (File.Exists(TestRepository.StoragePath))
            File.Delete(TestRepository.StoragePath);

        TestLoader.CommandExists("test").Should().BeFalse();
    }

    [Fact]
    public void JsonLoader_LoadCommand_Buffer()
    {
        TestLoader.SaveCommand(TestRepository.Command1);
        var loader = TestLoader;
        var command1 = loader.LoadCommand(TestRepository.Command1.Name);
        var command2 = loader.LoadCommand(TestRepository.Command1.Name);

        ReferenceEquals(command1, command2).Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_LoadCommand_NoBuffer()
    {
        TestLoader.SaveCommand(TestRepository.Command1);
        var command1 = TestLoader.LoadCommand(TestRepository.Command1.Name);
        var command2 = TestLoader.LoadCommand(TestRepository.Command1.Name);

        ReferenceEquals(command1, command2).Should().BeFalse();
    }

    [Fact]
    public void JsonLoader_LoadCommands()
    {
        var loader = TestLoader;
        loader.SaveCommand(TestRepository.Command1);
        loader.SaveCommand(TestRepository.Command2);
        loader.SaveCommand(TestRepository.Command3);

        var commands = loader.LoadCommands();

        commands.Should().HaveCount(3)
            .And.Contain(command => command.Name == TestRepository.Command1.Name)
            .And.Contain(command => command.Name == TestRepository.Command2.Name)
            .And.Contain(command => command.Name == TestRepository.Command3.Name);
    }

    [Fact]
    public void JsonLoader_LoadCommands_NoCommands()
    {
        if (File.Exists(TestRepository.StoragePath))
            File.Delete(TestRepository.StoragePath);

        TestLoader.LoadCommands().Should().BeNull();
    }

    [Fact]
    public void JsonLoader_DeleteCommand()
    {
        var loader = TestLoader;
        loader.SaveCommand(TestRepository.Command1);
        loader.DeleteCommand(TestRepository.Command1.Name);

        loader.CommandExists(TestRepository.Command1.Name)
            .Should().BeFalse();
    }

    [Fact]
    public void JsonLoader_DeleteCommand_UnknownCommand()
    {
        var act = () => TestLoader.DeleteCommand("unknown");

        act.Should().Throw<ArgumentException>();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(TestRepository.FolderName))
            Directory.Delete(TestRepository.FolderName, true);
    }

    [Fact]
    public void JsonLoader_CriticalException_IfDeserializationFails()
    {
        if (!Directory.Exists(TestRepository.FolderName))
            Directory.CreateDirectory(TestRepository.FolderName);
        File.WriteAllText(
            TestRepository.StoragePath,
            "{}");
        
        var act = TestLoader.LoadCommands;

        act.Should().Throw<CriticalException>();
    }
}
