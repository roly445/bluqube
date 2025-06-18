using BluQube.Tests.RequesterHelpers;

namespace BluQube.Tests.Extensions.ServiceCollectionExtensions;

public class AddBluQubeRequesters
{
    [Fact]
    public async Task AddsTheRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBluQubeRequesters();

        // Assert
        await Verify(services.Select(x => new
        {
            x.ServiceType,
            x.Lifetime,
            x.ImplementationType,
        }));
    }
}