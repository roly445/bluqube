using BluQube.Queries;
using BluQube.Tests.TestHelpers.Stubs;

namespace BluQube.Tests.Queries.GenericQueryProcessorTests;

public class Existence
{
    [Fact]
    public void GenericQueryProcessorDoesNotExistWhenNoAttributeIsPresent()
    {
        // Arrange
        var handlerType = typeof(GenericQueryProcessor<,>);
        var concreteType = handlerType.MakeGenericType(typeof(StubNoAttrQuery), typeof(StubQueryResult));

        // Act
        var exists = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Any(t => t == concreteType);

        // Assert
        Assert.False(exists);
    }
}