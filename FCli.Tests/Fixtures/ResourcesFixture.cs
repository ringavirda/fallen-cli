using FCli.Services.Abstractions;

using Moq;

namespace FCli.Tests.Fixtures;

public class ResourcesFixture : Mock<IResources>
{
    public ResourcesFixture()
    {
        Setup(res => res.GetLocalizedString(It.IsAny<string>()))
            .Returns("TestString");
    }
}