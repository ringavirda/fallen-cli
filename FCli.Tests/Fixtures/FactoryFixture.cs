using FCli.Models;
using FCli.Models.Dtos;
using FCli.Models.Types;
using FCli.Services.Abstractions;

using Moq;

namespace FCli.Tests.Fixtures;

public class FactoryFixture
    : Mock<ICommandFactory>
{
    public readonly Command Command1 = new()
    {
        Name = "test1",
        Path = "test/path",
        Type = CommandType.Script,
        Shell = ShellType.Powershell,
        Options = "--test option",
        Action = () => { }
    };
    public readonly Command Command2 = new()
    {
        Name = "test2",
        Path = "test/path",
        Type = CommandType.Executable,
        Options = "--test option",
        Action = () => { }
    };
    public readonly Command Command3 = new()
    {
        Name = "test3",
        Path = "http://url",
        Type = CommandType.Website,
        Options = "",
        Action = () => { }
    };
    public readonly Command CommandSavable;

    public FactoryFixture()
    {
        var config = new ConfigFixture();
        CommandSavable = new()
        {
            Name = "test-savable",
            Type = CommandType.Directory,
            Shell = ShellType.Powershell,
            Path = Path.Combine(
                config.TestFilesPath,
                config.BashScriptName),
            Options = "option",
            Action = () => { }
        };

        Setup(factory =>
                factory.Construct(Command1.Name)).Returns(Command1);
        Setup(factory =>
            factory.Construct(Command2.Name)).Returns(Command1);
        Setup(factory =>
            factory.Construct(Command3.Name)).Returns(Command2);
        Setup(factory => factory.Construct(
            It.Is<CommandAlterRequest>(req =>
                req.Name == Command1.Name
                && req.Type == Command1.Type)))
            .Returns(Command1);
        Setup(factory => factory.Construct(
            It.Is<CommandAlterRequest>(req =>
                req.Name == Command2.Name
                && req.Type == Command2.Type)))
            .Returns(Command2);
        Setup(factory => factory.Construct(
            It.Is<CommandAlterRequest>(req =>
                req.Name == Command3.Name
                && req.Type == Command3.Type)))
            .Returns(Command3);
        Setup(factory => factory.Construct(
            It.Is<CommandAlterRequest>(req =>
                req.Name == CommandSavable.Name)))
            .Returns(CommandSavable);
    }
}