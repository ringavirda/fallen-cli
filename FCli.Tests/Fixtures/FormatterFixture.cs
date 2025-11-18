using FCli.Services.Abstractions;

using Moq;

namespace FCli.Tests.Fixtures;

public class FormatterFixture : Mock<ICommandLineFormatter>
{
    public FormatterFixture()
    { }
}