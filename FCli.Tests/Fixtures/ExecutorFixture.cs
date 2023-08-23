using FCli.Services.Abstractions;

using Moq;

namespace FCli.Tests.Fixtures;

public class ExecutorFixture : Mock<IToolExecutor>
{
    public ExecutorFixture()
    {

    }
}