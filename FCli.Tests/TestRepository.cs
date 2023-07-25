using Moq;

using FCli.Models;
using FCli.Models.Tools;
using FCli.Services;
using FCli.Services.Data;

namespace FCli.Tests;

public static class TestRepository
{
    public readonly static string TestFolderName = "FCliTest";
    public readonly static string TestStorageFileName
        = $"{Guid.NewGuid()}.json";
    public readonly static string TestStoragePath = Path.Combine(TestFolderName, TestStorageFileName);

    public readonly static string TestFilesPath = Path.Combine(
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")),
        "TestFiles");
    public readonly static string TestCmdScriptName = "test_cmd.bat";
    public readonly static string TestPSScriptName = "test_powershell.ps1";
    public readonly static string TestBashScriptName = "test_bash.sh";
    public readonly static string TestExecutableName = "WpfGui.exe";
    public readonly static string TestTxtName = "test_txt.txt";

    public readonly static Command TestCommand = new()
    {
        Name = "test",
        Path = "test/path",
        Type = CommandType.Bash,
        Options = "--test option",
        Action = () => { }
    };

    public static Mock<IConfig> ConfigFake
    {
        get
        {
            var config = new Mock<IConfig>();
            config.SetupGet(dyn => dyn.AppFolderName)
                .Returns(TestFolderName);
            config.SetupGet(dyn => dyn.AppFolderPath)
                .Returns(TestFolderName);
            config.SetupGet(dyn => dyn.StorageFileName)
                .Returns(TestStorageFileName);
            config.SetupGet(dyn => dyn.StorageFilePath)
                .Returns(TestStoragePath);
            return config;
        }
    }
    public static Mock<ICommandLoader> CommandLoaderFake
    {
        get
        {
            var loader = new Mock<ICommandLoader>();
            loader.Setup(loader =>
                loader.LoadCommand(TestCommand.Name)).Returns(TestCommand);
            loader.Setup(loader => loader.LoadCommands())
                .Returns(new List<Command>() { TestCommand });
            loader.Setup(loader =>
                loader.CommandExists(TestCommand.Name)).Returns(true);
            return loader;
        }
    }
    public static Mock<ICommandFactory> CommandFactoryFake
    {
        get
        {
            var factory = new Mock<ICommandFactory>();
            factory.Setup(factory =>
                factory.Construct(TestCommand.Name)).Returns(TestCommand);
            factory.Setup(factory => factory.Construct(
                TestCommand.Name,
                TestCommand.Path,
                TestCommand.Type,
                TestCommand.Options)).Returns(TestCommand);
            factory.Setup(factory => factory.Construct(
                "loadingTest",
                Path.Combine(TestFilesPath, TestExecutableName),
                TestCommand.Type,
                TestCommand.Options)).Returns(TestCommand);
            return factory;
        }
    }
    public static Mock<IToolExecutor> ToolExecutorFake
    {
        get
        {
            var executor = new Mock<IToolExecutor>();
            executor.SetupGet(executor => executor.KnownTypeFlags)
                .Returns(new List<string>() { "script", "url", "exe" });
            executor.SetupGet(executor => executor.KnownTools)
                .Returns(new List<Tool>() {
                new AddTool(
                    executor.Object,
                    CommandFactoryFake.Object,
                    CommandLoaderFake.Object),
            new RemoveTool(CommandLoaderFake.Object),
            new ListTool(
                executor.Object,
                CommandLoaderFake.Object),
            new RunTool(
                executor.Object,
                CommandFactoryFake.Object)
                });
            return executor;
        }
    }
}
