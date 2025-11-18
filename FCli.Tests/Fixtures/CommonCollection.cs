namespace FCli.Tests.Fixtures;

[CollectionDefinition("Common")]
public class CommonCollection :
    ICollectionFixture<FormatterFixture>,
    ICollectionFixture<ResourcesFixture>,
    ICollectionFixture<ConfigFixture>
{ }