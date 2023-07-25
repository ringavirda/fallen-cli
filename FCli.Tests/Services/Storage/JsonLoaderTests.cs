using Moq;

using FCli.Services.Data;
using FCli.Models;
using FCli.Common.Exceptions;

namespace FCli.Tests.Services.Storage;

public class JsonLoaderTests : IDisposable
{
    private static JsonLoader TestLoader 
        => new (TestRepository.ConfigFake.Object);

    [Fact]
    public void JsonLoader_Create()
    {
        _ = new JsonLoader(TestRepository.ConfigFake.Object);

        Directory.Exists(TestRepository.TestFolderName).Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_SaveCommand()
    {
        TestLoader.SaveCommand(TestRepository.TestCommand);

        File.Exists(TestRepository.TestStoragePath).Should().BeTrue();
        TestLoader.CommandExists(TestRepository.TestCommand.Name).Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_CommandExists_Correctly()
    {
        var loader = TestLoader;
        loader.SaveCommand(TestRepository.TestCommand);

        loader.CommandExists(TestRepository.TestCommand.Name).Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_CommandExists_Fails()
    {
        if (File.Exists(TestRepository.TestStoragePath))
            File.Delete(TestRepository.TestStoragePath);

        TestLoader.CommandExists("test").Should().BeFalse();
    }

    [Fact]
    public void JsonLoader_LoadCommand_Buffer()
    {
        TestLoader.SaveCommand(TestRepository.TestCommand);
        var loader = TestLoader;
        var command1 = loader.LoadCommand(TestRepository.TestCommand.Name);
        var command2 = loader.LoadCommand(TestRepository.TestCommand.Name);

        ReferenceEquals(command1, command2).Should().BeTrue();
    }

    [Fact]
    public void JsonLoader_LoadCommand_NoBuffer()
    {
        TestLoader.SaveCommand(TestRepository.TestCommand);
        var command1 = TestLoader.LoadCommand(TestRepository.TestCommand.Name);
        var command2 = TestLoader.LoadCommand(TestRepository.TestCommand.Name);

        ReferenceEquals(command1, command2).Should().BeFalse();
    }

    [Fact]
    public void JsonLoader_LoadCommands()
    {
        var loader = TestLoader;
        loader.SaveCommand(TestRepository.TestCommand);
        loader.SaveCommand(TestRepository.TestCommand);
        loader.SaveCommand(TestRepository.TestCommand);

        var commands = loader.LoadCommands();

        commands.Should().HaveCount(3);
    }

    [Fact]
    public void JsonLoader_LoadCommands_NoCommands()
    {
        if (File.Exists(TestRepository.TestStoragePath))
            File.Delete(TestRepository.TestStoragePath);

        TestLoader.LoadCommands().Should().BeNull();
    }

    [Fact]
    public void JsonLoader_DeleteCommand()
    {
        var loader = TestLoader;
        loader.SaveCommand(TestRepository.TestCommand);
        loader.DeleteCommand(TestRepository.TestCommand.Name);

        loader.CommandExists(TestRepository.TestCommand.Name)
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
        if (Directory.Exists(TestRepository.TestFolderName))
            Directory.Delete(TestRepository.TestFolderName, true);
    }

    [Fact]
    public void JsonLoader_CriticalException_IfDeserializationFails()
    {
        if (!Directory.Exists(TestRepository.TestFolderName))
            Directory.CreateDirectory(TestRepository.TestFolderName);
        File.WriteAllText(
            TestRepository.TestStoragePath,
            "{}");
        
        var act = TestLoader.LoadCommands;

        act.Should().Throw<CriticalException>();
    }
}
