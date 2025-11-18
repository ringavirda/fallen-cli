using FCli.Models.Types;
using FCli.Services.Abstractions;
using FCli.Services.Tools;

using Moq;

namespace FCli.Tests.Fixtures;

public class ConfigFixture : Mock<IConfig>
{
    public readonly string FolderName = "FCliTest";
    public readonly string StorageFileName = $"{Guid.NewGuid()}.json";
    public readonly string StoragePath;
    public readonly string TestFilesPath = Path.Combine(
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..")),
        "TestFiles");
    public readonly string CmdScriptName = "test_cmd.bat";
    public readonly string PSScriptName = "test_powershell.ps1";
    public readonly string BashScriptName = "test_bash.sh";
    public readonly string ExecutableName = "WpfGui.exe";
    public readonly string TxtName = "test_txt.txt";

    public ConfigFixture()
    {
        StoragePath = Path.Combine(FolderName, StorageFileName);

        SetupGet(cnf => cnf.AppFolderName).Returns(FolderName);
        SetupGet(cnf => cnf.AppFolderPath).Returns(FolderName);
        SetupGet(cnf => cnf.StorageFileName).Returns(StorageFileName);
        SetupGet(cnf => cnf.StorageFilePath).Returns(StoragePath);
        SetupGet(cnf => cnf.KnownTools).Returns(
        [
            new AddTool(), new ListTool(), new RemoveTool(), new RunTool()
        ]);
        SetupGet(cnf => cnf.KnownCommands).Returns(
        [
            new ("exe",CommandType.Executable,false,".exe"),
            new ("url",CommandType.Website,false,null),
            new ("script",CommandType.Script,true,null),
            new ("dir",CommandType.Directory,false,null)
        ]);
        SetupGet(cnf => cnf.KnownShells).Returns(
        [
            new("bash", ShellType.Bash, ".sh"),
            new("cmd", ShellType.Cmd, ".bat"),
            new("powershell", ShellType.Powershell, ".ps1"),
            new("fish", ShellType.Fish, ".fish")
        ]);
    }
}