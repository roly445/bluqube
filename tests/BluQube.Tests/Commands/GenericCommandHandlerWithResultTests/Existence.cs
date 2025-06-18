using BluQube.Commands;
using BluQube.Tests.RequesterHelpers.Stubs;

namespace BluQube.Tests.Commands.GenericCommandHandlerWithResultTests;

public class Existence
{
    [Fact]
    public void GenericCommandHandlerDoesNotExistWhenNoAttributeIsPresent()
    {
        // Arrange
        var handlerType = typeof(GenericCommandHandler<,>);
        var concreteType = handlerType.MakeGenericType(typeof(StubNoAttrWithResultCommand), typeof(StubWithResultCommandResult));

        // Act
        var exists = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Any(t => t == concreteType);

        // Assert
        Assert.False(exists);
    }
}