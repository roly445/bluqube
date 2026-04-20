using BluQube.Queries;
using BluQube.Tests.RequesterHelpers.Stubs;

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
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch (System.Reflection.ReflectionTypeLoadException e)
                {
                    return e.Types.Where(t => t != null)!;
                }
            })
            .Any(t => t == concreteType);

        // Assert
        Assert.False(exists);
    }
}