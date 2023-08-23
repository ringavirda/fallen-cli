using FCli.Models;
using FCli.Services.Abstractions;

using Moq;

namespace FCli.Tests.Fixtures;

public class LoaderFixture : Mock<ICommandLoader>,
    IClassFixture<FactoryFixture>,
    IClassFixture<ConfigFixture>
{
    private readonly ConfigFixture _config;

    public LoaderFixture()
    {
        _config = new();
        var factory = new FactoryFixture();

        Setup(loader => loader.LoadCommand(factory.Command1.Name))
            .Returns(factory.Command1);
        Setup(loader => loader.LoadCommand(factory.Command2.Name))
            .Returns(factory.Command2);
        Setup(loader => loader.LoadCommand(factory.Command3.Name))
            .Returns(factory.Command3);
        Setup(loader => loader.LoadCommands())
            .Returns(new List<Command>()
            {
                factory.Command1,
                factory.Command2,
                factory.Command3
            });
        Setup(loader => loader.CommandExists(factory.Command1.Name))
            .Returns(true);
        Setup(loader => loader.CommandExists(factory.Command2.Name))
            .Returns(true);
        Setup(loader => loader.CommandExists(factory.Command3.Name))
            .Returns(true);
        Setup(loader => loader.CommandExists(factory.CommandSavable.Name))
            .Returns(false);
    }
}