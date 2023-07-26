using Moq;

using FCli.Models;
using FCli.Models.Tools;
using FCli.Services;
using FCli.Services.Config;
using FCli.Services.Data;
using FCli.Services.Format;
using FCli.Models.Types;
using System.Resources;

namespace FCli.Tests;

public static class TestRepository
{
    public readonly static string FolderName = "FCliTest";
    public readonly static string StorageFileName
        = $"{Guid.NewGuid()}.json";
    public readonly static string StoragePath = Path.Combine(FolderName, StorageFileName);

    public readonly static string TestFilesPath = Path.Combine(
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")),
        "TestFiles");
    public readonly static string CmdScriptName = "test_cmd.bat";
    public readonly static string PSScriptName = "test_powershell.ps1";
    public readonly static string BashScriptName = "test_bash.sh";
    public readonly static string ExecutableName = "WpfGui.exe";
    public readonly static string TxtName = "test_txt.txt";

    public readonly static Command Command1 = new()
    {
        Name = "test1",
        Path = "test/path",
        Type = CommandType.Script,
        Shell = ShellType.Powershell,
        Options = "--test option",
        Action = () => { }
    };
    public readonly static Command Command2 = new()
    {
        Name = "test2",
        Path = "test/path",
        Type = CommandType.Executable,
        Options = "--test option",
        Action = () => { }
    };
    public readonly static Command Command3 = new()
    {
        Name = "test3",
        Path = "http://url",
        Type = CommandType.Website,
        Options = "",
        Action = () => { }
    };
    public readonly static Command CommandSavable = new()
    {
        Name = "test-savable",
        Path = Path.Combine(TestFilesPath, BashScriptName),
        Type = CommandType.Directory,
        Options = "option",
        Action = () => { }
    };

    public static Mock<IConfig> ConfigFake
    {
        get
        {
            var config = new Mock<IConfig>();
            config.SetupGet(dyn => dyn.AppFolderName)
                .Returns(FolderName);
            config.SetupGet(dyn => dyn.AppFolderPath)
                .Returns(FolderName);
            config.SetupGet(dyn => dyn.StorageFileName)
                .Returns(StorageFileName);
            config.SetupGet(dyn => dyn.StorageFilePath)
                .Returns(StoragePath);
            return config;
        }
    }

    public static Mock<ICommandLineFormatter> FormatterFake
    {
        get
        {
            var formatter = new Mock<ICommandLineFormatter>();
            formatter
                .Setup(format => format.ReadUserInput("(yes/any)"))
                .Returns("yes");
            return formatter;
        }
    }

    public static Mock<ResourceManager> ResourcesFake
    {
        get
        {
            var resources = new Mock<ResourceManager>();
            resources.Setup(manager => manager.GetString(It.IsAny<string>()))
                .Returns("Test resource");
            return resources;
        }
    }

    public static Mock<ICommandLoader> CommandLoaderFake
    {
        get
        {
            var loader = new Mock<ICommandLoader>();
            loader.Setup(loader =>
                loader.LoadCommand(Command1.Name)).Returns(Command1);
            loader.Setup(loader =>
                loader.LoadCommand(Command2.Name)).Returns(Command2);
            loader.Setup(loader =>
                loader.LoadCommand(Command3.Name)).Returns(Command3);
            loader.Setup(loader => loader.LoadCommands())
                .Returns(new List<Command>() { Command1, Command2, Command3 });
            loader.Setup(loader =>
                loader.CommandExists(Command1.Name)).Returns(true);
            loader.Setup(loader =>
                loader.CommandExists(Command2.Name)).Returns(true);
            loader.Setup(loader =>
                loader.CommandExists(Command3.Name)).Returns(true);
            loader.Setup(loader =>
                loader.CommandExists(CommandSavable.Name)).Returns(false);
            return loader;
        }
    }
    public static Mock<ICommandFactory> CommandFactoryFake
    {
        get
        {
            var factory = new Mock<ICommandFactory>();
            factory.Setup(factory =>
                factory.Construct(Command1.Name)).Returns(Command1);
            factory.Setup(factory =>
                factory.Construct(Command2.Name)).Returns(Command1);
            factory.Setup(factory =>
                factory.Construct(Command3.Name)).Returns(Command2);
            factory.Setup(factory => factory.Construct(
                Command1.Name,
                Command1.Path,
                Command1.Type,
                Command1.Shell,
                Command1.Options)).Returns(Command1);
            factory.Setup(factory => factory.Construct(
                Command2.Name,
                Command2.Path,
                Command2.Type,
                Command2.Shell,
                Command2.Options)).Returns(Command2);
            factory.Setup(factory => factory.Construct(
                Command3.Name,
                Command3.Path,
                Command3.Type,
                Command3.Shell,
                Command3.Options)).Returns(Command3);
            factory.Setup(factory => factory.Construct(
                CommandSavable.Name,
                CommandSavable.Path,
                CommandSavable.Type,
                CommandSavable.Shell,
                CommandSavable.Options)).Returns(CommandSavable);
            return factory;
        }
    }
    public static Mock<IToolExecutor> ToolExecutorFake
    {
        get
        {
            var executor = new Mock<IToolExecutor>();
            executor.SetupGet(executor => executor.Tools)
                .Returns(new List<Tool>() {
                new AddTool(
                    FormatterFake.Object,
                    ResourcesFake.Object,
                    executor.Object,
                    CommandFactoryFake.Object,
                    CommandLoaderFake.Object,
                    ConfigFake.Object),
                new RemoveTool(
                    FormatterFake.Object,
                    ResourcesFake.Object,
                    CommandLoaderFake.Object),
                new ListTool(
                    FormatterFake.Object,
                    ResourcesFake.Object,
                    executor.Object,
                    CommandLoaderFake.Object,
                    ConfigFake.Object),
                new RunTool(
                    FormatterFake.Object,
                    ResourcesFake.Object,
                    CommandFactoryFake.Object,
                    ConfigFake.Object)
                });
            return executor;
        }
    }
}
