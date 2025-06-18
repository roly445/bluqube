using BluQube.Commands;
using BluQube.Tests.RequesterHelpers.Stubs;

namespace BluQube.Tests.Commands.GenericCommandHandlerTests;

public class Existence
{
    [Fact]
    public void GenericCommandHandlerDoesNotExistWhenNoAttributeIsPresent()
    {
        // Arrange
        var handlerType = typeof(GenericCommandHandler<>);
        var concreteType = handlerType.MakeGenericType(typeof(StubNoAttrCommand));

        // Act
        var exists = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Any(t => t == concreteType);

        // Assert
        Assert.False(exists);
    }
}